using System.IO;
using Top.Gltf;
using UnityEditor;

namespace Top.Assets.Gltf.Editor.Tests
{
    internal static class TestGlb
    {
        public static void Write(GltfDocument document, byte[] bin, string assetPath)
        {
            using (var stream = File.Create(assetPath))
            {
                GltfWriter.WriteGlb(document, bin, stream);
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
