using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ItemTypeTableTests
    {
        [Test]
        public void Reads_names_only()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/itemtype.txt"));
            var table = TableFile.Read<ItemTypeRecord>(stream);

            Assert.That(table[1].Name, Is.EqualTo("Sword"));
            Assert.That(table.Count, Is.GreaterThan(10));
        }

        [Test]
        public void Reads_the_corrected_headgear_type()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/itemtype.txt"));
            var table = TableFile.Read<ItemTypeRecord>(stream);
            var record = table[14];

            Assert.That(record.Name, Is.EqualTo("Headgear"));
            Assert.That((ItemType)record.Id, Is.EqualTo(ItemType.Headgear));
        }
    }
}
