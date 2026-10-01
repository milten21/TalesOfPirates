using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class DeleteCharacterRequestTests
    {
        private const byte CharacterIndex = 1;
        private const string Code = "123456";
        private const string IndexAndCodeBytes = "01" + "0007" + "313233343536" + "00";

        [Test]
        public void A_request_reads_the_character_index_and_then_the_code()
        {
            var read = DeleteCharacterRequest.Read(new PacketReader(Hex.Bytes(IndexAndCodeBytes)));

            Assert.That(read.CharacterIndex, Is.EqualTo(CharacterIndex));
            Assert.That(read.Code, Is.EqualTo(Code));
        }

        [Test]
        public void A_request_is_the_character_index_byte_and_the_code_as_plain_text()
        {
            Assert.That(WriteToBytes(new DeleteCharacterRequest(CharacterIndex, Code)),
                Is.EqualTo(Hex.Bytes(IndexAndCodeBytes)));
        }

        [Test]
        public void A_request_round_trips()
        {
            var read = DeleteCharacterRequest.Read(
                new PacketReader(WriteToBytes(new DeleteCharacterRequest(2, "987654321012"))));

            Assert.That(read.CharacterIndex, Is.EqualTo(2));
            Assert.That(read.Code, Is.EqualTo("987654321012"));
        }

        [Test]
        public void A_request_with_no_code_throws()
        {
            Assert.That(
                () => DeleteCharacterRequest.Read(new PacketReader(Hex.Bytes("01"))),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => DeleteCharacterRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static byte[] WriteToBytes(DeleteCharacterRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }
    }
}
