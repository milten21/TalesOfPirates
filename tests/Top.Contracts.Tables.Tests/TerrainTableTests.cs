using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Contracts.Tables.World;

namespace Top.Contracts.Tables.Tests
{
    public class TerrainTableTests
    {
        private static List<TerrainEntry> Read(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return new TableFormat().Read<TerrainEntry>(stream);
        }

        [Test]
        public void Json_reads_as_typed_entries()
        {
            var entries = Read("""
                [
                  { "id": 5, "texturePath": "textures/terrain/grass05.png", "leavesFootprints": true },
                  { "id": 22, "texturePath": "textures/terrain/brick06.png", "type": 3 }
                ]
                """);

            Assert.That(entries, Has.Count.EqualTo(2));
            Assert.That(entries[0].Id, Is.EqualTo(5));
            Assert.That(entries[0].TexturePath, Is.EqualTo("textures/terrain/grass05.png"));
            Assert.That(entries[0].LeavesFootprints, Is.True);
            Assert.That(entries[0].Type, Is.EqualTo(0), "an absent type reads as zero");
            Assert.That(entries[1].Type, Is.EqualTo(3));
            Assert.That(entries[1].LeavesFootprints, Is.False);
        }

        [Test]
        public void Texture_paths_are_indexed_by_terrain_id()
        {
            var table = new TerrainTable(Read("""
                [
                  { "id": 1, "texturePath": "textures/terrain/subtract.png" },
                  { "id": 3, "texturePath": "textures/terrain/brick04.png" }
                ]
                """));

            var paths = table.TexturePaths();

            Assert.That(paths, Has.Length.EqualTo(4), "the array reaches the highest id");
            Assert.That(paths[1], Is.EqualTo("textures/terrain/subtract.png"));
            Assert.That(paths[3], Is.EqualTo("textures/terrain/brick04.png"));
            Assert.That(paths[0], Is.Null, "id zero is no terrain at all");
            Assert.That(paths[2], Is.Null, "an id the table skips names nothing");
        }
    }
}
