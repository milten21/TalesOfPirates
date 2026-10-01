using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyMemberVitalsTests
    {
        private const int FairyLink = 16;
        private const int FairyAppearanceLink = 24;

        private const string Observer2Vitals =
            "00000004" + "0000006D" + "0000006D" + "0000001C" + "00000026" + "00000001";

        private const string Observer2Look =
            "0000010007D0000000000000000009FA00000000000000000000000000000000000100000000000100000000000000000001" +
            "2100000000000000012710271000000000000101000000000100000000000000000100240002002F00010000000000000000" +
            "0000000000000000000000000281000000000000000127102710000000000001010000000001000000000000000001002700" +
            "01002400020000000000000000000000000000000000000000270F0000000000000000000000000000000000000000000000" +
            "0100000000000000000000000000000000000000000000000000000800000000000000012710271000000000000101000000" +
            "00010000000000000000010022000E0023001200000000000000000000000000000000000000000000000000000000000000" +
            "000000000000000000000000000000000000000000000000000000000000E700000000000000010578138800F000F0000101" +
            "0000000001000000000003000001001A0003001C0004001D0001001B0002001E000500000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000014B20000000000000001" +
            "61A861A8000000000001010000000001000000000000000001001A0000001C0000001D0000001B0000001E00000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000";

        private const string T1Vitals =
            "0000000C" + "000000CD" + "000000CD" + "0000002C" + "0000002C" + "00000008";

        private const string T1Look =
            "0000010007D0000000000000000009FA00000000000000000000000000000000000100000000000100000000000000000010" +
            "D400000000000000012710271000000000000101000000000100000000000000000100240004002F00010000000000000000" +
            "00000000000000000000000010D6000000000000000127102710000000000001010000000001000000000000000001002700" +
            "02002400030000000000000000000000000000000000000000270F0000000000000000000000000000000000000000000000" +
            "0100000000000000000000000000000000000000000000000000000900000000000000012710271000000000000101000000" +
            "00010000000000000000010022001D0023002100000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000";

        private const string PinnedObserver2Hex = Observer2Vitals + Observer2Look;

        private const string PinnedT1Hex = T1Vitals + T1Look;

        [Test]
        public void Pinned_vitals_carry_the_member_id_the_vitals_and_the_level()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedObserver2Hex));

            var vitals = PartyMemberVitals.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(vitals.MemberId, Is.EqualTo(4u));
            Assert.That(vitals.Health, Is.EqualTo(109));
            Assert.That(vitals.MaxHealth, Is.EqualTo(109));
            Assert.That(vitals.Mana, Is.EqualTo(28));
            Assert.That(vitals.MaxMana, Is.EqualTo(38));
            Assert.That(vitals.Level, Is.EqualTo(1));
        }

        [Test]
        public void Pinned_vitals_carry_the_full_look_of_the_member()
        {
            var look = Read(PinnedObserver2Hex).Look;

            Assert.That(look.IsBoat, Is.False);
            Assert.That(look.TypeId, Is.EqualTo(1));
            Assert.That(look.HairId, Is.EqualTo(2000));
            Assert.That(look.Links[FairyLink].Id, Is.EqualTo(231));
            Assert.That(look.Links[FairyAppearanceLink].Id, Is.EqualTo(5298));
        }

        [Test]
        public void Pinned_vitals_of_a_member_with_no_fairy_end_on_their_last_byte()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedT1Hex));

            var vitals = PartyMemberVitals.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(vitals.MemberId, Is.EqualTo(12u));
            Assert.That(vitals.Level, Is.EqualTo(8));
            Assert.That(vitals.Look.Links[FairyLink].Id, Is.Zero);
        }

        [TestCase(PinnedObserver2Hex)]
        [TestCase(PinnedT1Hex)]
        public void Pinned_vitals_write_back_the_bytes_the_server_sent(string hex)
        {
            var packet = new PacketWriter();

            Read(hex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(hex)));
        }

        [Test]
        public void Vitals_that_run_out_of_bytes_throw()
        {
            var pinned = Hex.Bytes(PinnedObserver2Hex);

            Assert.That(
                () => PartyMemberVitals.Read(new PacketReader(pinned.Take(pinned.Length - 1).ToArray())),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void Vitals_with_no_bytes_throw()
        {
            Assert.That(
                () => PartyMemberVitals.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static PartyMemberVitals Read(string hex)
        {
            return PartyMemberVitals.Read(new PacketReader(Hex.Bytes(hex)));
        }
    }
}
