using System;
using System.Collections.Generic;
using System.IO;
using Top.Gltf;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="GltfImporter"/>. Lists each material the
    /// file declares, buffers pending remap edits until Apply, and offers
    /// Extract Materials, which writes them next to the .glb and optionally
    /// reuses same-named project materials instead of overwriting.
    /// </summary>
    [CustomEditor(typeof(GltfImporter))]
    public class GltfImporterEditor : ScriptedImporterEditor
    {
        private const string ReuseProjectMaterialsPref = "Top.GltfImporter.ReuseProjectMaterials";

        private string[] _materialNames = Array.Empty<string>();

        private readonly Dictionary<string, Material> _pending = new Dictionary<string, Material>();

        private bool _reuseProjectMaterials;

        public override void OnEnable()
        {
            base.OnEnable();

            _reuseProjectMaterials = EditorPrefs.GetBool(ReuseProjectMaterialsPref, true);

            LoadMaterialNames();
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                base.OnInspectorGUI();
                return;
            }

            var importer = (GltfImporter)target;

            EditorGUILayout.LabelField("Remapped Materials", EditorStyles.boldLabel);

            if (_materialNames.Length == 0)
            {
                EditorGUILayout.HelpBox("The file defines no materials.", MessageType.Info);
            }

            var map = importer.GetExternalObjectMap();

            foreach (var name in _materialNames)
            {
                if (!_pending.TryGetValue(name, out var current))
                {
                    map.TryGetValue(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), out var mapped);
                    current = mapped as Material;
                }

                var next = (Material)EditorGUILayout.ObjectField(name, current, typeof(Material), false);

                if (next != current)
                {
                    _pending[name] = next;
                }
            }

            var reuse = EditorGUILayout.ToggleLeft("Reuse project materials by name on extract",
                _reuseProjectMaterials);

            if (reuse != _reuseProjectMaterials)
            {
                _reuseProjectMaterials = reuse;

                EditorPrefs.SetBool(ReuseProjectMaterialsPref, reuse);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_pending.Count == 0))
                {
                    if (GUILayout.Button("Apply Remap"))
                    {
                        ApplyPending(importer);
                    }

                    if (GUILayout.Button("Revert Remap"))
                    {
                        _pending.Clear();
                    }
                }

                using (new EditorGUI.DisabledScope(_materialNames.Length == 0))
                {
                    if (GUILayout.Button("Extract Materials"))
                    {
                        ExtractMaterials(importer);
                    }
                }
            }

            EditorGUILayout.Space();
            ApplyRevertGUI();
        }

        private void LoadMaterialNames()
        {
            var importer = target as AssetImporter;

            try
            {
                using var stream = File.OpenRead(importer!.assetPath);
                var file = GltfReader.Read(stream);

                var materials = file.Document.Materials;
                _materialNames = new string[materials?.Count ?? 0];
                for (var i = 0; i < _materialNames.Length; i++)
                {
                    _materialNames[i] = materials![i].Name ?? $"material_{i}";
                }
            }
            catch (Exception)
            {
                _materialNames = Array.Empty<string>();
            }
        }

        private void ApplyPending(GltfImporter importer)
        {
            foreach (var entry in _pending)
            {
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), entry.Key);

                if (entry.Value == null)
                {
                    importer.RemoveRemap(identifier);
                }
                else
                {
                    importer.AddRemap(identifier, entry.Value);
                }
            }

            _pending.Clear();

            AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
            AssetDatabase.ImportAsset(importer.assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private void ExtractMaterials(GltfImporter importer)
        {
            _pending.Clear();

            var materialsDir = $"{Path.GetDirectoryName(importer.assetPath)?.Replace('\\', '/')}/Materials";

            GltfMaterialExtractor.Extract(importer.assetPath, materialsDir, _reuseProjectMaterials);
        }
    }
}
