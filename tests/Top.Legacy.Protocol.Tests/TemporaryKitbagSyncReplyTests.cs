using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class TemporaryKitbagSyncReplyTests
    {
        private const string EmptyTemporaryKitbag = "00" + "0010" + "FFFF";

        [Test]
        public void A_reply_is_an_init_sync_of_the_temporary_bag()
        {
            var packet = new PacketReader(Hex.Bytes(EmptyTemporaryKitbag));

            var reply = TemporaryKitbagSyncReply.Read(packet);

            Assert.That(reply.Kitbag.IsInit, Is.True);
            Assert.That(reply.Kitbag.Capacity, Is.EqualTo(16));
            Assert.That(reply.Kitbag.Slots, Is.Empty);
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_reply_writes_back_the_bytes_it_was_read_from()
        {
            var bytes = Hex.Bytes(EmptyTemporaryKitbag);
            var packet = new PacketWriter();

            TemporaryKitbagSyncReply.Read(new PacketReader(bytes)).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_reply_with_no_bytes_throws()
        {
            Assert.That(
                () => TemporaryKitbagSyncReply.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
