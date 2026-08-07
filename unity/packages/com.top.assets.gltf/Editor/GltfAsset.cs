using System.IO;
using System.Linq;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Provides utility methods for handling GLTF assets within the context of Unity editor scripts.
    /// This static class includes methods for reading, writing, and managing GLTF data and associated assets.
    /// </summary>
    public static class GltfAsset
    {
        public static GltfData Read(string assetPath)
        {
            using var stream = File.OpenRead(assetPath);
            var file = GltfReader.Read(stream);

            var directory = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

            return new GltfData(file, uri => File.ReadAllBytes(Path.Combine(directory, uri)));
        }

        public static T Write<T>(T asset, string assetPath) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            asset.name = Path.GetFileNameWithoutExtension(assetPath);

            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);

                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            AssetDatabase.CreateAsset(asset, assetPath);

            return asset;
        }

        public static string FileName(string name)
        {
            return Path.GetInvalidFileNameChars()
                .Aggregate(name, (current, invalid) => current.Replace(invalid, '_'));
        }
    }
}
