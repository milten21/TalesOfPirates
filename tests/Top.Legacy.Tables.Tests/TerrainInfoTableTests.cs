using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class TerrainInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/terraininfo.txt"));
            var table = TableFile.Read<TerrainInfoRecord>(stream);

            Assert.That(table[1].Name, Is.EqualTo("texture/terrain/subtract.bmp"));
            Assert.That(table[1].Type, Is.EqualTo(1));
            Assert.That(table[1].LeavesFootprints, Is.False);
            Assert.That(table[6].Type, Is.EqualTo(3));
            Assert.That(table[6].LeavesFootprints, Is.True);
        }
    }
}
