using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyInviteTests
    {
        private const string InviterName = "Anne";
        private const uint InviterId = 9;
        private const ushort Icon = 1200;

        private const string InviteBytes = "0005" + "416E6E65" + "00" + "00000009" + "04B0";

        [Test]
        public void An_invite_reads_the_inviter_name_id_and_icon()
        {
            var invite = PartyInvite.Read(new PacketReader(Hex.Bytes(InviteBytes)));

            Assert.That(invite.InviterName, Is.EqualTo(InviterName));
            Assert.That(invite.InviterId, Is.EqualTo(InviterId));
            Assert.That(invite.Icon, Is.EqualTo(Icon));
        }

        [Test]
        public void An_invite_writes_the_name_as_a_string_then_the_id_then_the_icon()
        {
            var packet = new PacketWriter();
            new PartyInvite(InviterName, InviterId, Icon).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(InviteBytes)));
        }

        [Test]
        public void An_invite_round_trips()
        {
            var packet = new PacketWriter();
            new PartyInvite("Blackbeard", 42, 7).Write(packet);

            var read = PartyInvite.Read(new PacketReader(packet.ToArray()));

            Assert.That(read.InviterName, Is.EqualTo("Blackbeard"));
            Assert.That(read.InviterId, Is.EqualTo(42u));
            Assert.That(read.Icon, Is.EqualTo(7));
        }

        [Test]
        public void An_invite_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyInvite.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
