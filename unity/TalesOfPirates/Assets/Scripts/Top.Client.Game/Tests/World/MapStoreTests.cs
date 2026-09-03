using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.World;
using Top.Content;
using Top.Contracts.Assets.Maps;
using UnityEngine;

namespace Top.Client.Game.Tests.World
{
    public class MapStoreTests
    {
        private static readonly Color32 Red = new Color32(255, 0, 0, 255);
        private static readonly Color32 Blue = new Color32(0, 0, 255, 255);
        private static readonly Color32 White = new Color32(255, 255, 255, 255);

        private static readonly string[] Terrains =
        {
            null, null, "textures/terrain/grass.png", "textures/terrain/sand.png",
        };

        private static byte[] Png(int size, Color32 color)
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

        private static MemoryContentSource Content()
        {
            var content = new MemoryContentSource();

            content.Add("textures/terrain/grass.png", Png(2, Red));
            content.Add("textures/terrain/sand.png", Png(4, Blue));
            content.Add(MapTexturePaths.Masks, Png(8, White));

            for (var frame = 0; frame < MapTexturePaths.WaterFrames; frame++)
            {
                content.Add(MapTexturePaths.WaterFrame(frame), Png(frame == 3 ? 4 : 2, Blue));
            }

            return content;
        }

        private static MapStore Store(MemoryContentSource content)
        {
            return new MapStore(content, Terrains, Shader.Find("Top/Terrain"), Shader.Find("Top/Water"));
        }

        [Test]
        public async Task Every_terrain_id_becomes_a_slice_at_the_largest_texture_size()
        {
            using var store = Store(Content());

            var material = await store.GetTerrainMaterial();
            var textures = (Texture2DArray)material.GetTexture("_Textures");

            Assert.That(textures.depth, Is.EqualTo(Terrains.Length), "the array reaches the highest terrain id");
            Assert.That(textures.width, Is.EqualTo(4));
            Assert.That(textures.height, Is.EqualTo(4));
            Assert.That(material.GetTexture("_Masks").width, Is.EqualTo(8));
        }

        [Test]
        public async Task Every_water_frame_becomes_a_slice_at_the_largest_frame_size()
        {
            using var store = Store(Content());

            var material = await store.GetWaterMaterial();
            var frames = (Texture2DArray)material.GetTexture("_Frames");

            Assert.That(frames.depth, Is.EqualTo(MapTexturePaths.WaterFrames));
            Assert.That(frames.width, Is.EqualTo(4));
            Assert.That(frames.height, Is.EqualTo(4));
        }

        [Test]
        public async Task The_materials_are_built_once_and_shared()
        {
            using var store = Store(Content());

            Assert.That(await store.GetTerrainMaterial(), Is.SameAs(await store.GetTerrainMaterial()));
            Assert.That(await store.GetWaterMaterial(), Is.SameAs(await store.GetWaterMaterial()));
        }

        [Test]
        public void Map_content_the_tree_does_not_hold_fails_the_load()
        {
            using var store = Store(Content());

            Assert.ThrowsAsync<System.IO.FileNotFoundException>(() => store.Load("maps/missing.map"));
        }

        [Test]
        public async Task A_map_reads_back_through_the_content_port()
        {
            var map = new MapFile(4, 4, 2);
            var content = Content();

            map.Chunks[0, 0] = new MapChunk(map.ChunkSize);

            using var stream = new System.IO.MemoryStream();

            map.Write(stream);
            content.Add("maps/test.map", stream.ToArray());

            using var store = Store(content);

            var data = await store.Load("maps/test.map");

            Assert.That(data.Width, Is.EqualTo(4));
            Assert.That(data.HasChunk(0, 0), Is.True);
            Assert.That(data.HasChunk(1, 1), Is.False);
        }
    }
}
