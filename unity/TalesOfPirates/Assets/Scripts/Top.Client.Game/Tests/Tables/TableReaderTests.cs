using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.Tables;
using Top.Content;
using Top.Contracts;
using Top.Contracts.Tables.World;

namespace Top.Client.Game.Tests.Tables
{
    public class TableReaderTests
    {
        private static TableReader CreateReader(string sceneObjects, string terrains = "[]", string maps = "[]")
        {
            var content = new MemoryContentSource();

            content.Add(SceneObjectTable.Path, Encoding.UTF8.GetBytes(sceneObjects));
            content.Add(TerrainTable.Path, Encoding.UTF8.GetBytes(terrains));
            content.Add(MapTable.Path, Encoding.UTF8.GetBytes(maps));

            return new TableReader(content);
        }

        [Test]
        public async Task Table_bytes_read_into_lookups_by_id()
        {
            var tables = await CreateReader(
                "[{\"id\":42,\"modelPath\":\"models/scene/stone01.glb\"}," +
                "{\"id\":7,\"kind\":\"PointLight\",\"color\":{\"r\":255,\"g\":128,\"b\":0}}]").Read();

            Assert.That(tables.SceneObjectTable.Count, Is.EqualTo(2));
            Assert.That(tables.SceneObjectTable[42].ModelPath, Is.EqualTo("models/scene/stone01.glb"));
            Assert.That(tables.SceneObjectTable[7].ModelPath, Is.Null, "an emitter row carries no path");
            Assert.That(((PointLightEntry)tables.SceneObjectTable[7]).Color, Is.EqualTo(new Rgb(255, 128, 0)));
        }

        [Test]
        public async Task Terrain_bytes_read_into_texture_paths_by_id()
        {
            var tables = await CreateReader("[]",
                "[{\"id\":5,\"texturePath\":\"textures/terrain/grass05.png\"}]").Read();

            Assert.That(tables.TerrainTable[5].TexturePath, Is.EqualTo("textures/terrain/grass05.png"));
        }

        [Test]
        public async Task Map_bytes_read_into_converted_map_paths_by_id()
        {
            var tables = await CreateReader("[]", maps:
                "[{\"id\":1,\"name\":\"garner\",\"mapPath\":\"maps/garner.map\",\"displayName\":\"Ascaron\"}]").Read();

            Assert.That(tables.MapTable[1].MapPath, Is.EqualTo("maps/garner.map"));
            Assert.That(tables.MapTable[1].Name, Is.EqualTo("garner"));
            Assert.That(tables.MapTable[1].DisplayName, Is.EqualTo("Ascaron"));
        }

        [Test]
        public async Task An_unknown_id_looks_up_as_absent()
        {
            var tables = await CreateReader("[{\"id\":42}]").Read();

            Assert.That(tables.SceneObjectTable.TryGetById(9000, out _), Is.False);
        }

        [Test]
        public void Missing_table_content_fails_the_read()
        {
            Assert.ThrowsAsync<FileNotFoundException>(() => new TableReader(new MemoryContentSource()).Read());
        }
    }
}
