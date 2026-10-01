using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class ChangeSecondPasswordReplyTests
    {
        private const ushort Changed = 0;
        private const ushort WrongSecondPassword = 534;

        [Test]
        public void A_reply_decodes_the_success_code()
        {
            var reply = ChangeSecondPasswordReply.Read(new PacketReader(Hex.Bytes("0000")));

            Assert.That(reply.Code, Is.EqualTo(Changed));
        }

        [Test]
        public void A_reply_the_group_server_refused_decodes_its_code()
        {
            var reply = ChangeSecondPasswordReply.Read(new PacketReader(Hex.Bytes("0216")));

            Assert.That(reply.Code, Is.EqualTo(WrongSecondPassword));
        }

        [Test]
        public void A_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            new ChangeSecondPasswordReply(WrongSecondPassword).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes("0216")));
        }

        [Test]
        public void A_reply_with_no_bytes_throws()
        {
            Assert.That(
                () => ChangeSecondPasswordReply.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
