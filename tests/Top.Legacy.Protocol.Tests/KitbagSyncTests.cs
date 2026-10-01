using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class KitbagSyncTests
    {
        private const string InitType = "00";

        private const string SwitchType = "05";

        private const string Capacity = "0018";

        private const string EndOfList = "FFFF";

        private const string SwordInFirstSlot =
            "0000" + "0121" + "00000BEE" + "0000" + "0001" + "2710" + "2710" + "0000" + "0000" + "00" + "01" + "01" +
            "00000000" + "00000000" + "00000000" + "00";

        private const string EmptySixthSlot = "0005" + "0000";

        [Test]
        public void An_init_sync_carries_the_capacity_before_the_slots()
        {
            var packet = new PacketReader(Hex.Bytes(InitType + Capacity + SwordInFirstSlot + EndOfList));

            var sync = KitbagSync.Read(packet, KitbagSlot.DefaultBoatItemIds);

            Assert.That(sync.IsInit, Is.True);
            Assert.That(sync.Capacity, Is.EqualTo(24));
            Assert.That(sync.Slots[0].GridId, Is.Zero);
            Assert.That(sync.Slots[0].Content.Id, Is.EqualTo(289));
            Assert.That(sync.Slots[0].Content.DbId, Is.EqualTo(0xBEE));
            Assert.That(sync.Slots[0].Content.Endure, Is.EqualTo(new short[] { 10000, 10000 }));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void Any_other_sync_carries_no_capacity()
        {
            var packet = new PacketReader(Hex.Bytes(SwitchType + SwordInFirstSlot + EmptySixthSlot + EndOfList));

            var sync = KitbagSync.Read(packet, KitbagSlot.DefaultBoatItemIds);

            Assert.That(sync.IsInit, Is.False);
            Assert.That(sync.Type, Is.EqualTo(5));
            Assert.That(sync.Capacity, Is.Zero);
            Assert.That(sync.Slots, Has.Count.EqualTo(2));
            Assert.That(sync.Slots[1].GridId, Is.EqualTo(5));
            Assert.That(sync.Slots[1].Content.IsEmpty, Is.True);
            Assert.That(packet.Remaining, Is.Zero);
        }

        [TestCase(InitType + Capacity + SwordInFirstSlot + EndOfList)]
        [TestCase(SwitchType + SwordInFirstSlot + EmptySixthSlot + EndOfList)]
        [TestCase(SwitchType + EndOfList)]
        public void A_sync_writes_back_the_bytes_it_was_read_from(string hex)
        {
            var bytes = Hex.Bytes(hex);
            var packet = new PacketWriter();

            KitbagSync.Read(new PacketReader(bytes), KitbagSlot.DefaultBoatItemIds).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_capacity_outside_an_init_sync_is_refused()
        {
            Assert.That(() => new KitbagSync(5, 24, Array.Empty<KitbagSlot>()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void A_sync_without_its_terminator_throws()
        {
            Assert.That(
                () => KitbagSync.Read(new PacketReader(Hex.Bytes(SwitchType + SwordInFirstSlot)),
                    KitbagSlot.DefaultBoatItemIds),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
