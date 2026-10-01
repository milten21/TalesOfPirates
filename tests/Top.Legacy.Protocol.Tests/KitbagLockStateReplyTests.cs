using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class KitbagLockStateReplyTests
    {
        [TestCase("00", false)]
        [TestCase("01", true)]
        public void A_reply_reads_the_lock_state_from_one_byte(string bytes, bool isLocked)
        {
            Assert.That(KitbagLockStateReply.Read(new PacketReader(Hex.Bytes(bytes))).IsLocked, Is.EqualTo(isLocked));
        }

        [TestCase(false, "00")]
        [TestCase(true, "01")]
        public void A_reply_writes_the_byte_the_server_sends(bool isLocked, string bytes)
        {
            var packet = new PacketWriter();
            new KitbagLockStateReply(isLocked).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(bytes)));
        }

        [Test]
        public void A_reply_with_no_bytes_throws()
        {
            Assert.That(() => KitbagLockStateReply.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
