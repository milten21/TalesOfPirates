using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.World.Terrain;
using Top.Content;
using Top.Contracts.Assets.Maps;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.Tests.World.Terrain
{
    public class TerrainMaterialTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);
        private static readonly Color32 Blue = new Color32(0, 0, 255, 255);
        private static readonly Color32 White = new Color32(255, 255, 255, 255);

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

        private static MemoryContentSource CreateContent()
        {
            var content = new MemoryContentSource();

            content.Add("textures/terrain/grass.png", EncodePng(2, Red));
            content.Add("textures/terrain/sand.png", EncodePng(4, Blue));
            content.Add(MapTexturePaths.MaskAtlas, EncodePng(8, White));

            return content;
        }

        [Test]
        public async Task Every_terrain_id_becomes_a_slice_at_the_largest_texture_size()
        {
            var terrains = new TerrainTable(new[]
            {
                new TerrainEntry { Id = 2, TexturePath = "textures/terrain/grass.png" },
                new TerrainEntry { Id = 3, TexturePath = "textures/terrain/sand.png" },
            });

            using var terrainMaterial = await TerrainMaterial.Load(CreateContent(), terrains, Shader.Find("Top/Terrain"));

            var textures = (Texture2DArray)terrainMaterial.Material.GetTexture("_Textures");

            Assert.That(textures.depth, Is.EqualTo(4), "the array reaches the highest terrain id");
            Assert.That(textures.width, Is.EqualTo(4));
            Assert.That(textures.height, Is.EqualTo(4));
            Assert.That(terrainMaterial.Material.GetTexture("_Masks").width, Is.EqualTo(8));
        }

        [Test]
        public async Task Terrain_id_zero_names_no_terrain_and_reads_no_texture()
        {
            var terrains = new TerrainTable(new[]
            {
                new TerrainEntry { Id = 0, TexturePath = MapTexturePaths.MaskAtlas },
                new TerrainEntry { Id = 1, TexturePath = "textures/terrain/grass.png" },
            });

            using var terrainMaterial = await TerrainMaterial.Load(CreateContent(), terrains, Shader.Find("Top/Terrain"));

            var textures = (Texture2DArray)terrainMaterial.Material.GetTexture("_Textures");

            Assert.That(textures.depth, Is.EqualTo(2));
            Assert.That(textures.width, Is.EqualTo(2), "the 8x8 image at id zero was never read");
        }
    }
}
