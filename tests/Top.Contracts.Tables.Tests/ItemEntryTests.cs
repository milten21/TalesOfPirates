using NUnit.Framework;
using Top.Contracts.Tables.Items;

namespace Top.Contracts.Tables.Tests
{
    public class ItemEntryTests
    {
        private static readonly ItemEntry Hair = new ItemEntry
        {
            Id = 2000,
            EquipLinks = new[] { 0 },
            ModelPaths = new[] { "models/character/0000000001.glb", null, "models/character/0002000001.glb", null },
        };

        [Test]
        public void A_body_type_wears_the_model_of_its_own_body_type()
        {
            Assert.That(Hair.ModelPathFor(0), Is.EqualTo("models/character/0000000001.glb"));
            Assert.That(Hair.ModelPathFor(2), Is.EqualTo("models/character/0002000001.glb"));
        }

        [Test]
        public void A_body_type_the_item_has_no_mesh_for_gets_no_model()
        {
            Assert.That(Hair.ModelPathFor(1), Is.Null);
        }

        [Test]
        public void A_body_type_outside_the_model_paths_gets_no_model()
        {
            Assert.That(Hair.ModelPathFor(4), Is.Null);
            Assert.That(Hair.ModelPathFor(-1), Is.Null);
        }

        [Test]
        public void An_item_without_model_paths_gets_no_model()
        {
            Assert.That(new ItemEntry { Id = 3000, EquipLinks = new[] { 2 } }.ModelPathFor(0), Is.Null);
        }

        [Test]
        public void An_item_equips_at_each_of_its_equip_links_and_nowhere_else()
        {
            var sword = new ItemEntry { Id = 1, EquipLinks = new[] { 9, 6 } };

            Assert.That(sword.CanEquipAt(9), Is.True);
            Assert.That(sword.CanEquipAt(6), Is.True);
            Assert.That(sword.CanEquipAt(2), Is.False);
        }

        [Test]
        public void An_item_without_equip_links_equips_nowhere()
        {
            Assert.That(new ItemEntry { Id = 3000 }.CanEquipAt(0), Is.False);
        }
    }
}
