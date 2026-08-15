using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Content.Packs;
using Top.Contracts.Tables.World;

namespace Top.Client.Definitions.Tests
{
    public class DefinitionsStoreTests
    {
        private static DefinitionsStore Store(string sceneObjects, string terrains = "[]", string maps = "[]")
        {
            var content = new MemoryContent();

            content.Add(SceneObjectTable.Path, Encoding.UTF8.GetBytes(sceneObjects));
            content.Add(TerrainTable.Path, Encoding.UTF8.GetBytes(terrains));
            content.Add(MapTable.Path, Encoding.UTF8.GetBytes(maps));

            return new DefinitionsStore(content);
        }

        [Test]
        public async Task Table_bytes_load_into_lookups_by_id()
        {
            var definitions = await Store(
                "[{\"id\":42,\"modelPath\":\"models/scene/stone01.glb\"}," +
                "{\"id\":7,\"type\":3,\"color\":[255,128,0]}]").Load();

            Assert.That(definitions.SceneObjects.Count, Is.EqualTo(2));
            Assert.That(definitions.SceneObjects[42].ModelPath, Is.EqualTo("models/scene/stone01.glb"));
            Assert.That(definitions.SceneObjects[7].ModelPath, Is.Null, "an emitter row carries no path");
            Assert.That(((PointLightEntry)definitions.SceneObjects[7]).Color, Is.EqualTo(new[] { 255, 128, 0 }));
        }

        [Test]
        public async Task Terrain_bytes_load_into_texture_paths_by_id()
        {
            var definitions = await Store("[]",
                "[{\"id\":5,\"texturePath\":\"textures/terrain/grass05.png\"}]").Load();

            Assert.That(definitions.Terrains[5].TexturePath, Is.EqualTo("textures/terrain/grass05.png"));
            Assert.That(definitions.Terrains.TexturePaths()[5], Is.EqualTo("textures/terrain/grass05.png"));
        }

        [Test]
        public async Task Map_bytes_load_into_converted_map_paths_by_id()
        {
            var definitions = await Store("[]", maps:
                "[{\"id\":1,\"name\":\"garner\",\"mapPath\":\"maps/garner.map\",\"displayName\":\"Ascaron\"}]").Load();

            Assert.That(definitions.Maps[1].MapPath, Is.EqualTo("maps/garner.map"));
            Assert.That(definitions.Maps[1].Name, Is.EqualTo("garner"));
            Assert.That(definitions.Maps[1].DisplayName, Is.EqualTo("Ascaron"));
        }

        [Test]
        public async Task An_unknown_id_looks_up_as_absent()
        {
            var definitions = await Store("[{\"id\":42}]").Load();

            Assert.That(definitions.SceneObjects.TryGetById(9000, out _), Is.False);
        }

        [Test]
        public void Missing_table_content_fails_the_load()
        {
            Assert.ThrowsAsync<FileNotFoundException>(() => new DefinitionsStore(new MemoryContent()).Load());
        }
    }
}
