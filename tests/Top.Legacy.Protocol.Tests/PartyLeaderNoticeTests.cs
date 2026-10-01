using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyLeaderNoticeTests
    {
        private const string PinnedMemberLedByT1Hex = "00000004" + "0000000C";

        private const string PinnedT1AfterThePartyEndedHex = "0000000C" + "00000000";

        [Test]
        public void A_pinned_notice_names_the_character_and_its_leader()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedMemberLedByT1Hex));

            var notice = PartyLeaderNotice.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(notice.WorldId, Is.EqualTo(4u));
            Assert.That(notice.LeaderId, Is.EqualTo(12u));
        }

        [Test]
        public void A_pinned_notice_after_the_party_ended_carries_no_leader()
        {
            var notice = PartyLeaderNotice.Read(new PacketReader(Hex.Bytes(PinnedT1AfterThePartyEndedHex)));

            Assert.That(notice.WorldId, Is.EqualTo(12u));
            Assert.That(notice.LeaderId, Is.Zero);
        }

        [Test]
        public void A_notice_writes_the_world_id_then_the_leader_id()
        {
            var packet = new PacketWriter();

            new PartyLeaderNotice(4, 12).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedMemberLedByT1Hex)));
        }

        [Test]
        public void A_notice_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyLeaderNotice.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
