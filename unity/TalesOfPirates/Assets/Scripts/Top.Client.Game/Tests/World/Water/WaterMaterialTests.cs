using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.World.Water;
using Top.Content;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.Tests.World.Water
{
    public class WaterMaterialTests
    {
        private static readonly Color32 Blue = new Color32(0, 0, 255, 255);

        private static byte[] EncodePng(int size, Color32 color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var png = texture.EncodeToPNG();

            Object.DestroyImmediate(texture);

            return png;
        }

        [Test]
        public async Task Every_water_frame_becomes_a_slice_at_the_largest_frame_size()
        {
            var content = new MemoryContentSource();

            for (var frame = 0; frame < MapTexturePaths.WaterFrameCount; frame++)
            {
                content.Add(MapTexturePaths.GetWaterFramePath(frame), EncodePng(frame == 3 ? 4 : 2, Blue));
            }

            using var waterMaterial = await WaterMaterial.Load(content, Shader.Find("Top/Water"));

            var frames = (Texture2DArray)waterMaterial.Material.GetTexture("_Frames");

            Assert.That(frames.depth, Is.EqualTo(MapTexturePaths.WaterFrameCount));
            Assert.That(frames.width, Is.EqualTo(4));
            Assert.That(frames.height, Is.EqualTo(4));
        }
    }
}
