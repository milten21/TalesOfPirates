using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyRefuseTests
    {
        private const uint InviterId = 9;

        private const string RefuseBytes = "00000009";

        [Test]
        public void A_refuse_reads_the_inviter_id()
        {
            Assert.That(PartyRefuse.Read(new PacketReader(Hex.Bytes(RefuseBytes))).InviterId, Is.EqualTo(InviterId));
        }

        [Test]
        public void A_refuse_is_the_inviter_id_and_nothing_else()
        {
            var packet = new PacketWriter();
            new PartyRefuse(InviterId).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(RefuseBytes)));
        }

        [Test]
        public void A_refuse_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyRefuse.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
