using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyKickTests
    {
        private const uint MemberId = 12;

        private const string KickBytes = "0000000C";

        [Test]
        public void A_kick_reads_the_member_id()
        {
            Assert.That(PartyKick.Read(new PacketReader(Hex.Bytes(KickBytes))).MemberId, Is.EqualTo(MemberId));
        }

        [Test]
        public void A_kick_is_the_member_id_and_nothing_else()
        {
            var packet = new PacketWriter();
            new PartyKick(MemberId).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(KickBytes)));
        }

        [Test]
        public void A_kick_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyKick.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
