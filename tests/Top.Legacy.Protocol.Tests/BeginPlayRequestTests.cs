using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class BeginPlayRequestTests
    {
        [Test]
        public void A_request_is_one_byte_carrying_the_character_index()
        {
            Assert.That(WriteToBytes(new BeginPlayRequest(2)), Is.EqualTo(new byte[] { 0x02 }));
        }

        [Test]
        public void A_request_round_trips()
        {
            var read = BeginPlayRequest.Read(new PacketReader(WriteToBytes(new BeginPlayRequest(1))));

            Assert.That(read.CharacterIndex, Is.EqualTo(1));
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => BeginPlayRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static byte[] WriteToBytes(BeginPlayRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }
    }
}
