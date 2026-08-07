using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Gltf;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Unity <c>ScriptedImporter</c> for .glb and .gltf files. Reads the
    /// document, builds it with <see cref="GltfObjectBuilder"/>, and registers
    /// each mesh and animation clip as a sub-asset. Materials go through the
    /// external-object remap so the inspector can point them at project assets.
    /// </summary>
    [ScriptedImporter(1, new[] { "glb", "gltf" })]
    public class GltfImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using var stream = File.OpenRead(ctx.assetPath);

            var file = GltfReader.Read(stream);

            var directory = Path.GetDirectoryName(ctx.assetPath);
            var data = new GltfData(file, uri =>
            {
                var path = Path.Combine(directory, uri);
                ctx.DependsOnSourceAsset(path);
                return File.ReadAllBytes(path);
            });

            var preview = PreviewMaterial();
            var root = GltfObjectBuilder.Build(data, Path.GetFileNameWithoutExtension(ctx.assetPath),
                Materials(data.Document, preview), preview);

            var usedIds = new HashSet<string>();

            foreach (var mesh in Meshes(root))
            {
                var id = mesh.name;

                while (!usedIds.Add(id))
                {
                    id += "_";
                }

                ctx.AddObjectToAsset(id, mesh);
            }

            foreach (var clip in Clips(root))
            {
                ctx.AddObjectToAsset("anim_" + clip.name, clip);
            }

            ctx.AddObjectToAsset("root", root);
            ctx.SetMainObject(root);
        }

        private Material[] Materials(GltfDocument document, Material preview)
        {
            if (document.Materials == null)
            {
                return Array.Empty<Material>();
            }

            var externalObjects = GetExternalObjectMap();

            return document.Materials
                .Select((declared, i) =>
                    externalObjects.TryGetValue(
                        new SourceAssetIdentifier(typeof(Material), declared.Name ?? $"material_{i}"),
                        out var mapped) && mapped is Material material
                        ? material
                        : preview)
                .ToArray();
        }

        private static IEnumerable<Mesh> Meshes(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true)
                .Select(filter => filter.sharedMesh);
            var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Select(renderer => renderer.sharedMesh);

            return filters.Concat(skinned).Where(mesh => mesh != null).Distinct();
        }

        private static IEnumerable<AnimationClip> Clips(GameObject root)
        {
            return root.GetComponentsInChildren<Animation>(true)
                .SelectMany(animation => AnimationUtility.GetAnimationClips(animation.gameObject))
                .Where(clip => clip != null)
                .Distinct();
        }

        private static Material PreviewMaterial()
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

            return pipeline != null
                ? pipeline.defaultMaterial
                : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
        }
    }
}
