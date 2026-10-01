using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class KitbagSlotTests
    {
        private const short VesselDeed = 3988;
        private const short Sword = 289;

        [Test]
        public void A_boat_item_reads_the_world_id_of_its_boat_before_the_forge()
        {
            var read = KitbagSlot.ReadList(new PacketReader(WriteToBytes(BuildDeed())), KitbagSlot.DefaultBoatItemIds);

            Assert.That(read[0].Content.BoatWorldId, Is.EqualTo(77));
            Assert.That(read[0].Content.InstanceId, Is.EqualTo(5));
        }

        [Test]
        public void The_boat_items_are_whatever_the_caller_names()
        {
            Assert.That(
                () => KitbagSlot.ReadList(new PacketReader(WriteToBytes(BuildDeed())), Array.Empty<short>()),
                Throws.InstanceOf<InvalidDataException>(),
                "read as an ordinary item, the deed's extra long throws the grid after it off");
        }

        [Test]
        public void The_reference_build_types_one_item_as_a_boat()
        {
            Assert.That(KitbagSlot.DefaultBoatItemIds, Is.EqualTo(new[] { VesselDeed }));
        }

        [Test]
        public void An_ordinary_item_carries_no_boat_world_id()
        {
            var sword = new ItemGrid(
                Sword, 0, 0, 1, new short[] { 9999, 10000 }, new short[] { 0, 0 }, 0, true, true, 0, null, 0, 0, null);

            var read = KitbagSlot.ReadList(new PacketReader(WriteToBytes(sword)), KitbagSlot.DefaultBoatItemIds);

            Assert.That(read[0].Content.BoatWorldId, Is.Null);
            Assert.That(read[0].Content.Endure, Is.EqualTo(new short[] { 9999, 10000 }));
        }

        [Test]
        public void A_list_ends_on_the_terminator_the_server_writes()
        {
            var packet = new PacketReader(WriteToBytes(BuildDeed()));

            KitbagSlot.ReadList(packet, KitbagSlot.DefaultBoatItemIds);

            Assert.That(packet.Remaining, Is.Zero);
        }

        private static ItemGrid BuildDeed()
        {
            return new ItemGrid(
                VesselDeed, 0, 0, 1, new short[] { 0, 0 }, new short[] { 0, 0 }, 0, true, true, 0, 77, 0, 5, null);
        }

        private static byte[] WriteToBytes(ItemGrid content)
        {
            var packet = new PacketWriter();
            KitbagSlot.WriteList(packet, new[] { new KitbagSlot(0, content) });

            return packet.ToArray();
        }
    }
}
