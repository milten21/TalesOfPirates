using NUnit.Framework;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ItemModulesTests
    {
        private static ItemInfoRecord MakeItem(params string[] modules)
        {
            return new ItemInfoRecord { Id = 464, Name = "Transparent Clothes", Modules = modules };
        }

        [Test]
        public void Resolves_the_per_framework_module_column()
        {
            var item = MakeItem("10110002", "0000610002", "0001610002", "0002610002", "0003610002");

            Assert.That(ItemModules.TryGetModule(item, 0, out var lance), Is.True);
            Assert.That(lance, Is.EqualTo("0000610002"));
            Assert.That(ItemModules.TryGetModule(item, 3, out var ami), Is.True);
            Assert.That(ami, Is.EqualTo("0003610002"));
        }

        [Test]
        public void Zero_and_blank_modules_mean_no_model()
        {
            var item = MakeItem("10100001", "01010001", "0", "", null);

            Assert.That(ItemModules.TryGetModule(item, 1, out _), Is.False);
            Assert.That(ItemModules.TryGetModule(item, 2, out _), Is.False);
            Assert.That(ItemModules.TryGetModule(item, 3, out _), Is.False);
        }

        [Test]
        public void Out_of_range_model_and_missing_modules_resolve_false()
        {
            Assert.That(ItemModules.TryGetModule(MakeItem("10100001", "01010001"), 1, out _), Is.False);
            Assert.That(ItemModules.TryGetModule(new ItemInfoRecord { Id = 1 }, 0, out _), Is.False);
            Assert.That(ItemModules.TryGetModule(null, 0, out _), Is.False);
            Assert.That(ItemModules.TryGetModule(MakeItem("a", "b", "c", "d", "e"), -1, out _), Is.False);
        }

        [Test]
        public void The_body_column_names_the_frameworks_that_equip_the_item_counting_from_one()
        {
            var item = new ItemInfoRecord { Id = 303, Body = [1, 3] };

            Assert.That(ItemModules.CanEquip(item, 0), Is.True);
            Assert.That(ItemModules.CanEquip(item, 1), Is.False);
            Assert.That(ItemModules.CanEquip(item, 2), Is.True);
            Assert.That(ItemModules.CanEquip(item, 3), Is.False);
        }

        [Test]
        public void A_body_column_opening_with_minus_one_lets_every_framework_equip_the_item()
        {
            var item = new ItemInfoRecord { Id = 5748, Body = [-1, -2, -2, -2] };

            Assert.That(ItemModules.CanEquip(item, 0), Is.True);
            Assert.That(ItemModules.CanEquip(item, 3), Is.True);
        }

        [Test]
        public void No_framework_equips_an_item_with_no_body_column()
        {
            Assert.That(ItemModules.CanEquip(new ItemInfoRecord { Id = 1 }, 0), Is.False);
            Assert.That(ItemModules.CanEquip(new ItemInfoRecord { Id = 1, Body = [] }, 0), Is.False);
            Assert.That(ItemModules.CanEquip(null, 0), Is.False);
        }
    }
}
