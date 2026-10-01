using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyRefreshTests
    {
        private const string T1 = "0000000C" + "0003" + "543100" + "000100" + "0001";

        private const string Observer2 = "00000004" + "000A" + "4F627365727665723200" + "000100" + "0001";

        private const string PinnedJoinHex = "01" + "02" + T1 + Observer2;

        private const string PinnedMemberLeavesHex = "02" + "02" + T1 + Observer2;

        private const string PinnedKickHex = "06" + "02" + T1 + Observer2;

        private const string PinnedLeaderLeavesHex = "02" + "02" + Observer2 + T1;

        [Test]
        public void A_pinned_join_lists_the_members_in_join_order()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedJoinHex));

            var refresh = PartyRefresh.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(refresh.Kind, Is.EqualTo(PartyRefresh.Join));
            Assert.That(refresh.Members.Select(member => member.CharacterId), Is.EqualTo(new uint[] { 12, 4 }));
            Assert.That(refresh.Members.Select(member => member.Name), Is.EqualTo(new[] { "T1", "Observer2" }));
            Assert.That(refresh.Members.Select(member => member.Motto), Is.EqualTo(new[] { string.Empty, string.Empty }));
            Assert.That(refresh.Members.Select(member => member.Icon), Is.EqualTo(new ushort[] { 1, 1 }));
        }

        [Test]
        public void A_pinned_leave_lists_the_member_that_left_last()
        {
            var refresh = Read(PinnedMemberLeavesHex);

            Assert.That(refresh.Kind, Is.EqualTo(PartyRefresh.Leave));
            Assert.That(refresh.Members.Select(member => member.Name), Is.EqualTo(new[] { "T1", "Observer2" }));
        }

        [Test]
        public void A_pinned_kick_lists_the_kicked_member_last()
        {
            var refresh = Read(PinnedKickHex);

            Assert.That(refresh.Kind, Is.EqualTo(PartyRefresh.Kick));
            Assert.That(refresh.Members.Select(member => member.Name), Is.EqualTo(new[] { "T1", "Observer2" }));
        }

        [Test]
        public void A_pinned_leave_of_the_leader_lists_the_leader_last()
        {
            var refresh = Read(PinnedLeaderLeavesHex);

            Assert.That(refresh.Kind, Is.EqualTo(PartyRefresh.Leave));
            Assert.That(refresh.Members.Select(member => member.Name), Is.EqualTo(new[] { "Observer2", "T1" }));
        }

        [TestCase(PinnedJoinHex)]
        [TestCase(PinnedLeaderLeavesHex)]
        public void A_pinned_refresh_writes_back_the_bytes_the_server_sent(string hex)
        {
            var packet = new PacketWriter();

            Read(hex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(hex)));
        }

        [Test]
        public void A_refresh_that_runs_out_of_bytes_throws()
        {
            var pinned = Hex.Bytes(PinnedJoinHex);

            Assert.That(
                () => PartyRefresh.Read(new PacketReader(pinned.Take(pinned.Length - 1).ToArray())),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_refresh_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyRefresh.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static PartyRefresh Read(string hex)
        {
            return PartyRefresh.Read(new PacketReader(Hex.Bytes(hex)));
        }
    }
}
