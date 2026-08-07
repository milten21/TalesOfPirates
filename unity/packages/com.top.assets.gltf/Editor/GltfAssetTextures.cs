using System;
using System.Collections.Generic;
using System.IO;
using Top.Gltf;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Editor
{
    /// <summary>
    /// Resolves each glTF image to a Unity texture asset for the model beside
    /// the .glb: external URIs load straight from their project path, embedded
    /// images extract once into a Textures folder next to the .glb and reimport
    /// with alpha-is-transparency enabled. Results cache by image index.
    /// </summary>
    public class GltfAssetTextures
    {
        private readonly Dictionary<int, Texture2D> _byImage = new Dictionary<int, Texture2D>();
        private readonly GltfData _data;
        private readonly string _glbAssetPath;

        public GltfAssetTextures(GltfData data, string glbAssetPath)
        {
            _data = data;
            _glbAssetPath = glbAssetPath;
        }

        private GltfDocument Document => _data.Document;

        public Texture2D OfMaterial(GltfMaterial material)
        {
            var info = material.PbrMetallicRoughness?.BaseColorTexture;

            return info == null ? null : Of(info.Index);
        }

        public Texture2D Of(int textureIndex)
        {
            if (Document.Textures == null || textureIndex < 0 || textureIndex >= Document.Textures.Count
                || Document.Textures[textureIndex].Source == null)
            {
                Log.Warning($"texture {textureIndex} has no image source");
                return null;
            }

            var imageIndex = Document.Textures[textureIndex].Source.Value;

            if (_byImage.TryGetValue(imageIndex, out var cached))
            {
                return cached;
            }

            var image = Document.Images[imageIndex];
            string assetPath;

            if (image.BufferView != null)
            {
                assetPath = ExtractEmbeddedImage(image, imageIndex);
            }
            else if (!string.IsNullOrEmpty(image.Uri) && !image.Uri.StartsWith("data:", StringComparison.Ordinal))
            {
                var glbDir = Path.GetDirectoryName(Path.GetFullPath(_glbAssetPath));
                assetPath = ToAssetPath(Path.Combine(glbDir, Uri.UnescapeDataString(image.Uri)));
            }
            else
            {
                Log.Warning($"image {imageIndex} has an unsupported source");
                _byImage[imageIndex] = null;
                return null;
            }

            var texture = assetPath != null
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
                : null;

            if (texture == null)
            {
                Log.Warning($"image {imageIndex} did not resolve to a texture asset");
            }

            _byImage[imageIndex] = texture;

            return texture;
        }

        private string ExtractEmbeddedImage(GltfImage image, int imageIndex)
        {
            var extension = image.MimeType == "image/jpeg" ? ".jpg" : ".png";
            var name = image.Name != null
                ? GltfAsset.FileName(image.Name)
                : $"{Path.GetFileNameWithoutExtension(_glbAssetPath)}_image_{imageIndex}";

            var directory = $"{Path.GetDirectoryName(_glbAssetPath)?.Replace('\\', '/')}/Textures";
            var assetPath = $"{directory}/{name}{extension}";

            if (!File.Exists(assetPath))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(assetPath, _data.ReadBufferView(image.BufferView.Value));
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter textureImporter &&
                    !textureImporter.alphaIsTransparency)
                {
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.SaveAndReimport();
                }
            }

            return assetPath;
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            var full = Path.GetFullPath(fullPath).Replace('\\', '/');

            return full.StartsWith(projectRoot + "/", StringComparison.Ordinal)
                ? full.Substring(projectRoot.Length + 1)
                : null;
        }
    }
}
