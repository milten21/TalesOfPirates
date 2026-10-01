using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class BeginPlayReplyTests
    {
        private const ushort TargetMapNotReached = 4;
        private const ushort InvalidCharacter = 524;

        [Test]
        public void A_reply_decodes_the_code_the_gate_sent()
        {
            var reply = BeginPlayReply.Read(new PacketReader(Hex.Bytes("0004")));

            Assert.That(reply.Code, Is.EqualTo(TargetMapNotReached));
        }

        [Test]
        public void A_reply_the_group_server_answered_decodes_its_code()
        {
            var reply = BeginPlayReply.Read(new PacketReader(Hex.Bytes("020C")));

            Assert.That(reply.Code, Is.EqualTo(InvalidCharacter));
        }

        [Test]
        public void A_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            new BeginPlayReply(TargetMapNotReached).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes("0004")));
        }

        [Test]
        public void A_reply_with_no_bytes_throws()
        {
            Assert.That(
                () => BeginPlayReply.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
