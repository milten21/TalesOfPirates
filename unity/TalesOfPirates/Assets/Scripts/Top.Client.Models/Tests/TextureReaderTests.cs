using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Models.Textures;
using Top.Content;
using UnityEngine;

namespace Top.Client.Models.Tests
{
    public class TextureReaderTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);
        private static readonly Color32 Blue = new Color32(0, 0, 255, 255);

        private static Texture2D CreateTexture(int size, Color32 left, Color32 right)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[(y * size) + x] = x < size / 2 ? left : right;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }

        [Test]
        public void Fitting_a_texture_to_a_larger_size_keeps_its_picture()
        {
            var texture = CreateTexture(2, Red, Blue);

            var pixels = new TextureReader(new MemoryContentSource()).Resize(texture, 4);

            Object.DestroyImmediate(texture);

            Assert.That(pixels.Length, Is.EqualTo(16));
            Assert.That(pixels[0].r, Is.GreaterThan(pixels[0].b), "the left edge stays red");
            Assert.That(pixels[3].b, Is.GreaterThan(pixels[3].r), "the right edge stays blue");
            Assert.That(pixels[15].b, Is.GreaterThan(pixels[15].r));
        }

        [Test]
        public async Task An_image_the_content_does_not_hold_reads_as_nothing()
        {
            var images = new TextureReader(new MemoryContentSource());

            Assert.That(await images.Read("missing.png"), Is.Null);
        }
    }
}
