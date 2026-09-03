using System.IO;
using System.Numerics;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class MapInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/mapinfo.txt"));
            var table = TableFile.Read<MapInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("garner"));
            Assert.That(record.DisplayName, Is.EqualTo("Ascaron"));
            Assert.That(record.ShowSwitch, Is.True);
            Assert.That(record.InitX, Is.EqualTo(2202));
            Assert.That(record.InitY, Is.EqualTo(2782));
            Assert.That(record.LightColor, Is.EqualTo(new[] { 255, 255, 255 }));
            Assert.That(record.LightDirection, Is.EqualTo(new Vector3(1f, 1f, -1f)),
                "the row's own column repeats the start pair, so the default stands");
        }
    }
}
