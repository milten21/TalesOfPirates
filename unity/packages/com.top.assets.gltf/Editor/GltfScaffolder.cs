using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Contract.Models;
using Top.Assets.Contract.Models.Extras;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Provides utilities to scaffold GLTF assets, including material extraction,
    /// texture management, and prefab creation.
    /// </summary>
    public static class GltfScaffolder
    {
        public static void Scaffold(string glbAssetPath, string prefabPath, bool overwrite,
            Action<GameObject> decorate = null)
        {
            var modelDir = Path.GetDirectoryName(glbAssetPath)?.Replace('\\', '/');
            var data = GltfAsset.Read(glbAssetPath);
            var textures = new GltfAssetTextures(data, glbAssetPath);

            GltfMaterialExtractor.Extract(textures, data.Document, glbAssetPath,
                $"{modelDir}/Materials", reuseProjectMaterials: false, overwrite);

            var tracks = CreateTracks(data.Document, textures,
                Path.GetFileNameWithoutExtension(glbAssetPath), $"{modelDir}/Tracks",
                overwrite);

            BuildPrefab(glbAssetPath, prefabPath, data.Document, tracks, decorate);

            AssetDatabase.SaveAssets();
        }

        private class MaterialTracks
        {
            public UvAnimationTrack Uv;
            public OpacityAnimationTrack Opacity;
            public TextureImageTrack Flipbook;

            public bool Any => Uv != null || Opacity != null || Flipbook != null;
        }

        private static Dictionary<int, MaterialTracks> CreateTracks(GltfDocument document,
            GltfAssetTextures textures, string modelName, string tracksDir, bool overwrite)
        {
            var result = new Dictionary<int, MaterialTracks>();

            for (var i = 0; i < (document.Materials?.Count ?? 0); i++)
            {
                if (!MaterialExtras.TryRead(document.Materials[i], out var extras))
                {
                    continue;
                }

                var name = document.Materials[i].Name ?? $"material_{i}";
                var tracks = new MaterialTracks();

                if (extras.UvAnimation != null)
                {
                    tracks.Uv = Save(TrackMapper.CreateUvTrack(extras.UvAnimation),
                        TrackPath(tracksDir, modelName, name, "uv"), overwrite);
                }

                if (extras.OpacityAnimation != null)
                {
                    tracks.Opacity = Save(TrackMapper.CreateOpacityTrack(extras.OpacityAnimation),
                        TrackPath(tracksDir, modelName, name, "opacity"), overwrite);
                }

                if (extras.Flipbook != null)
                {
                    tracks.Flipbook = Save(
                        TrackMapper.CreateTextureImageTrack(extras.Flipbook, textures.Of),
                        TrackPath(tracksDir, modelName, name, "teximg"), overwrite);
                }

                if (tracks.Any)
                {
                    result[i] = tracks;
                }
            }

            return result;
        }

        private static string TrackPath(string tracksDir, string modelName, string materialName, string kind)
        {
            return Naming.TryParseMaterial(materialName, out var objectId, out var subset)
                ? $"{tracksDir}/{modelName}_{kind}_{objectId}_{subset}.asset"
                : $"{tracksDir}/{GltfAsset.FileName(materialName)}_{kind}.asset";
        }

        private static T Save<T>(T track, string assetPath, bool overwrite) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (existing != null && !overwrite)
            {
                UnityEngine.Object.DestroyImmediate(track);

                return existing;
            }

            return GltfAsset.Write(track, assetPath);
        }

        private static void BuildPrefab(string glbAssetPath, string prefabPath, GltfDocument document,
            Dictionary<int, MaterialTracks> tracks, Action<GameObject> decorate)
        {
            var model = AssetDatabase.LoadMainAssetAtPath(glbAssetPath) as GameObject;

            if (model == null)
            {
                Log.Warning($"no imported model at '{glbAssetPath}'");

                return;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(model);

            try
            {
                AddTrackComponents(root, document, tracks);

                foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if (Naming.IsHelper(meshFilter.gameObject.name))
                    {
                        meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh =
                            meshFilter.sharedMesh;
                    }
                }

                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (Naming.IsLitShell(transform.name))
                    {
                        transform.gameObject.SetActive(false);
                    }
                }

                decorate?.Invoke(root);

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AddTrackComponents(GameObject root, GltfDocument document,
            Dictionary<int, MaterialTracks> tracks)
        {
            var bindings = MaterialBinding.In(document);

            foreach (var entry in tracks)
            {
                if (!bindings.Contains(entry.Key))
                {
                    Log.Warning($"nothing renders animated material {entry.Key}; its tracks stay unplayed");

                    continue;
                }

                foreach (var binding in bindings[entry.Key])
                {
                    var target = FindDeep(root.transform, binding.NodeName);

                    if (target == null)
                    {
                        Log.Warning($"animated node {binding.NodeName} not found");

                        continue;
                    }

                    Install(target.gameObject, binding.SubMesh, entry.Value);
                }
            }
        }

        private static void Install(GameObject target, int subMesh, MaterialTracks tracks)
        {
            if (tracks.Uv != null)
            {
                var uv = target.AddComponent<UvMatrixAnimation>();
                uv.materialIndex = subMesh;
                uv.track = tracks.Uv;
            }

            if (tracks.Opacity != null)
            {
                var opacity = target.AddComponent<OpacityAnimation>();
                opacity.materialIndex = subMesh;
                opacity.track = tracks.Opacity;
            }

            if (tracks.Flipbook != null)
            {
                var flipbook = target.AddComponent<TextureImageAnimation>();
                flipbook.materialIndex = subMesh;
                flipbook.track = tracks.Flipbook;
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform != root && transform.name == name);
        }
    }
}
