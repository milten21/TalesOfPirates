using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class CreateSecondPasswordReplyTests
    {
        private const ushort Created = 0;
        private const ushort ServerBusy = 532;

        [Test]
        public void A_reply_decodes_the_success_code()
        {
            var reply = CreateSecondPasswordReply.Read(new PacketReader(Hex.Bytes("0000")));

            Assert.That(reply.Code, Is.EqualTo(Created));
        }

        [Test]
        public void A_reply_the_group_server_refused_decodes_its_code()
        {
            var reply = CreateSecondPasswordReply.Read(new PacketReader(Hex.Bytes("0214")));

            Assert.That(reply.Code, Is.EqualTo(ServerBusy));
        }

        [Test]
        public void A_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            new CreateSecondPasswordReply(ServerBusy).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes("0214")));
        }

        [Test]
        public void A_reply_with_no_bytes_throws()
        {
            Assert.That(
                () => CreateSecondPasswordReply.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
