using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ItemInfoTableTests
    {
        private static string BuildRow(int id, string attrEffect, string cooldown)
        {
            var fields = new string[95];
            fields[0] = id.ToString();
            fields[1] = "Synthetic Item";

            for (int i = 2; i < fields.Length; i++)
            {
                fields[i] = "0";
            }

            fields[86] = attrEffect;
            fields[94] = cooldown;

            return string.Join("\t", fields) + "\n";
        }

        private static Table<ItemInfoRecord> ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return TableFile.Read<ItemInfoRecord>(stream);
        }

        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/iteminfo.txt"));
            var table = TableFile.Read<ItemInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Short Sword"));
            Assert.That(record.Icon, Is.EqualTo("w0001"));
            Assert.That(record.Type, Is.EqualTo(ItemType.Sword));
            Assert.That(record.Price, Is.EqualTo(1015));
            Assert.That(record.NeedLevel, Is.EqualTo(10));
            Assert.That(record.Jobs, Is.EqualTo(new[] { 1, 8, 9, 10 }));
            Assert.That(record.EquipSlots, Is.EqualTo(new[] { 9, 6 }));
            Assert.That(record.AttackSpeedCoef, Is.EqualTo(50));
            Assert.That(record.MinAttackValue, Is.EqualTo(new[] { 30, 58 }));
            Assert.That(record.MaxAttackValue, Is.EqualTo(new[] { 38, 70 }));
            Assert.That(record.Endure, Is.EqualTo(new[] { 10000, 10000 }));
            Assert.That(record.Holes, Is.EqualTo(3));
            Assert.That(record.AttrEffect, Is.EqualTo("0"));
            Assert.That(record.Description, Does.StartWith("Sword that is slightly shorter"));
            Assert.That(record.Cooldown, Is.EqualTo(1f));
        }

        [Test]
        public void Reads_the_corrected_axe_type()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/iteminfo.txt"));
            var table = TableFile.Read<ItemInfoRecord>(stream);

            var record = table[196];

            Assert.That(record.Name, Is.EqualTo("Axe"));
            Assert.That(record.Type, Is.EqualTo(ItemType.Axe));
        }

        [Test]
        public void Reads_the_corrected_metal_pickaxe_type()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/iteminfo.txt"));
            var table = TableFile.Read<ItemInfoRecord>(stream);

            var record = table[197];

            Assert.That(record.Name, Is.EqualTo("Pickaxe"));
            Assert.That(record.Type, Is.EqualTo(ItemType.MetalPickaxe));
        }

        [Test]
        public void Reads_attr_effect_on_item_use_row()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/iteminfo.txt"));
            var table = TableFile.Read<ItemInfoRecord>(stream);

            var record = table[24];

            Assert.That(record.AttrEffect, Is.EqualTo("ItemUse_IDBOX"));
        }

        [Test]
        public void Cooldown_reads_the_raw_column_when_it_is_not_zero()
        {
            var table = ReadText(BuildRow(201, "0", "2.5"));

            var record = table[201];

            Assert.That(record.Cooldown, Is.EqualTo(2.5f).Within(1e-5f));
        }

        [Test]
        public void Cooldown_falls_back_to_one_when_raw_column_is_zero()
        {
            var table = ReadText(BuildRow(202, "0", "0"));

            var record = table[202];

            Assert.That(record.Cooldown, Is.EqualTo(1f));
        }
    }
}
