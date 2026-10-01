using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class EndPlayReplyTests
    {
        private const string Success = "0000";
        private const string SlotCapOfNine = "09";
        private const string TwoRows = "02";

        private const string TestPirateEquipment =
            "000009FA10D4000002810000270F000000000009" +
            "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        private const string PirateEquipment =
            "000009FC" +
            "00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000";

        private const string TestPirateRow =
            "01" + "000B5465737450697261746500" + "00074E6577626965" + "00" + "000C" + "0001" + TestPirateEquipment;

        private const string PirateRow =
            "01" + "0007506972617465" + "00" + "00033F3F00" + "0000" + "0001" + PirateEquipment;

        private const string PinnedReplyHex = Success + SlotCapOfNine + TwoRows + TestPirateRow + PirateRow;

        private const ushort NotPlaying = 3;

        [Test]
        public void A_pinned_reply_decodes_its_slot_cap_and_its_characters()
        {
            var reply = Read(PinnedReplyHex);

            Assert.That(reply.IsRefused, Is.False);
            Assert.That(reply.SlotCap, Is.EqualTo(9));
            Assert.That(ListNames(reply), Is.EqualTo(new[] { "TestPirate", "Pirate" }));
            Assert.That(reply.Characters[0].Job, Is.EqualTo("Newbie"));
            Assert.That(reply.Characters[0].Level, Is.EqualTo(12));
            Assert.That(reply.Characters[0].TypeId, Is.EqualTo(1));
            Assert.That(reply.Characters[1].Level, Is.EqualTo(0));
        }

        [Test]
        public void A_pinned_reply_ends_on_its_last_byte()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedReplyHex));

            EndPlayReply.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_refused_reply_decodes_its_code_and_stops()
        {
            var reply = Read("0003");

            Assert.That(reply.IsRefused, Is.True);
            Assert.That(reply.Code, Is.EqualTo(NotPlaying));
            Assert.That(reply.Characters, Is.Empty);
        }

        [Test]
        public void An_accepted_reply_round_trips()
        {
            var read = RoundTrip(EndPlayReply.Accepted(9, Read(PinnedReplyHex).Characters));

            Assert.That(read.SlotCap, Is.EqualTo(9));
            Assert.That(ListNames(read), Is.EqualTo(new[] { "TestPirate", "Pirate" }));
        }

        [Test]
        public void An_accepted_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            Read(PinnedReplyHex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedReplyHex)));
        }

        [Test]
        public void A_refused_reply_writes_its_code_only()
        {
            var packet = new PacketWriter();
            EndPlayReply.Refused(NotPlaying).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes("0003")));
        }

        [Test]
        public void A_reply_that_runs_out_of_bytes_throws()
        {
            var pinned = Hex.Bytes(PinnedReplyHex);
            var truncated = new byte[pinned.Length - 1];
            Array.Copy(pinned, truncated, truncated.Length);

            Assert.That(
                () => EndPlayReply.Read(new PacketReader(truncated)),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_refusal_is_not_built_from_the_code_of_an_accepted_reply()
        {
            Assert.That(() => EndPlayReply.Refused(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        private static EndPlayReply Read(string hex)
        {
            return EndPlayReply.Read(new PacketReader(Hex.Bytes(hex)));
        }

        private static EndPlayReply RoundTrip(EndPlayReply reply)
        {
            var packet = new PacketWriter();
            reply.Write(packet);

            return EndPlayReply.Read(new PacketReader(packet.ToArray()));
        }

        private static string[] ListNames(EndPlayReply reply)
        {
            return reply.Characters.Select(character => character.Name).ToArray();
        }
    }
}
