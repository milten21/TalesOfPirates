using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class CreateSecondPasswordRequestTests
    {
        private const string Code = "123456";
        private const string CodeBytes = "0007" + "313233343536" + "00";

        [Test]
        public void A_request_reads_the_code_from_one_string()
        {
            var read = CreateSecondPasswordRequest.Read(new PacketReader(Hex.Bytes(CodeBytes)));

            Assert.That(read.Code, Is.EqualTo(Code));
        }

        [Test]
        public void A_request_is_one_string_carrying_the_code_as_plain_text()
        {
            Assert.That(WriteToBytes(new CreateSecondPasswordRequest(Code)), Is.EqualTo(Hex.Bytes(CodeBytes)));
        }

        [Test]
        public void A_request_round_trips()
        {
            var read = CreateSecondPasswordRequest.Read(
                new PacketReader(WriteToBytes(new CreateSecondPasswordRequest("987654321012"))));

            Assert.That(read.Code, Is.EqualTo("987654321012"));
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => CreateSecondPasswordRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static byte[] WriteToBytes(CreateSecondPasswordRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }
    }
}
