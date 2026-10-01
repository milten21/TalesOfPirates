using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyAcceptTests
    {
        private const uint InviterId = 9;

        private const string AcceptBytes = "00000009";

        [Test]
        public void An_accept_reads_the_inviter_id()
        {
            Assert.That(PartyAccept.Read(new PacketReader(Hex.Bytes(AcceptBytes))).InviterId, Is.EqualTo(InviterId));
        }

        [Test]
        public void An_accept_is_the_inviter_id_and_nothing_else()
        {
            var packet = new PacketWriter();
            new PartyAccept(InviterId).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(AcceptBytes)));
        }

        [Test]
        public void An_accept_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyAccept.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
