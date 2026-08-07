using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Turns the materials a document declares into project assets and remaps
    /// the model onto them. What each material asks for comes from the
    /// document alone, resolved once and shared with the textures it binds.
    /// </summary>
    public static class GltfMaterialExtractor
    {
        public static void Extract(string glbAssetPath, string materialsDir,
            bool reuseProjectMaterials = true, bool overwrite = false)
        {
            var data = GltfAsset.Read(glbAssetPath);

            Extract(new GltfAssetTextures(data, glbAssetPath), data.Document, glbAssetPath, materialsDir,
                reuseProjectMaterials, overwrite);
        }

        internal static void Extract(GltfAssetTextures textures, GltfDocument document,
            string glbAssetPath, string materialsDir, bool reuseProjectMaterials, bool overwrite)
        {
            if (document.Materials == null || document.Materials.Count == 0)
            {
                Log.Warning($"'{glbAssetPath}' defines no materials");
                return;
            }

            var importer = AssetImporter.GetAtPath(glbAssetPath);
            var cutoffs = new Dictionary<Texture2D, float>();

            for (var i = 0; i < document.Materials.Count; i++)
            {
                var declared = document.Materials[i];
                var name = declared.Name ?? $"material_{i}";
                var assetPath = $"{materialsDir}/{GltfAsset.FileName(name)}.mat";
                var state = GltfMaterialMapper.Map(declared);
                var texture = textures.OfMaterial(declared);
                var material = overwrite ? null : AssetDatabase.LoadAssetAtPath<Material>(assetPath);

                NoteCutoff(cutoffs, state, texture);

                if (material == null && reuseProjectMaterials)
                {
                    material = FindProjectMaterial(name);
                }

                if (material == null)
                {
                    material = GltfAsset.Write(RenderStateMapper.CreateMaterial(state, texture), assetPath);
                }

                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), material);
            }

            ApplyCutoffs(cutoffs);

            AssetDatabase.WriteImportSettingsIfDirty(glbAssetPath);
            AssetDatabase.ImportAsset(glbAssetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static void NoteCutoff(Dictionary<Texture2D, float> cutoffs,
            RenderState state, Texture2D texture)
        {
            if (state.AlphaTest && texture != null
                                && (!cutoffs.TryGetValue(texture, out var lowest) || state.Cutoff < lowest))
            {
                cutoffs[texture] = state.Cutoff;
            }
        }

        private static void ApplyCutoffs(Dictionary<Texture2D, float> cutoffs)
        {
            foreach (var pair in cutoffs)
            {
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(pair.Key)) is TextureImporter importer
                    && (!importer.mipMapsPreserveCoverage
                        || !Mathf.Approximately(importer.alphaTestReferenceValue, pair.Value)))
                {
                    importer.mipMapsPreserveCoverage = true;
                    importer.alphaTestReferenceValue = pair.Value;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Material FindProjectMaterial(string name)
        {
            var fileName = GltfAsset.FileName(name);

            var matches = AssetDatabase.FindAssets($"t:Material {fileName}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == fileName)
                .ToList();

            if (matches.Count == 0)
            {
                return null;
            }

            if (matches.Count > 1)
            {
                Log.Warning($"{name}: multiple project materials share the name; using '{matches[0]}'");
            }

            return AssetDatabase.LoadAssetAtPath<Material>(matches[0]);
        }
    }
}
