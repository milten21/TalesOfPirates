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
    }
}
