using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class ChangeSecondPasswordRequestTests
    {
        private const string OldCode = "123456";
        private const string NewCode = "65432100";
        private const string CodesBytes = "0007" + "313233343536" + "00" + "0009" + "3635343332313030" + "00";

        [Test]
        public void A_request_reads_the_old_code_and_then_the_new_code()
        {
            var read = ChangeSecondPasswordRequest.Read(new PacketReader(Hex.Bytes(CodesBytes)));

            Assert.That(read.OldCode, Is.EqualTo(OldCode));
            Assert.That(read.NewCode, Is.EqualTo(NewCode));
        }

        [Test]
        public void A_request_is_two_strings_carrying_the_codes_as_plain_text()
        {
            Assert.That(WriteToBytes(new ChangeSecondPasswordRequest(OldCode, NewCode)),
                Is.EqualTo(Hex.Bytes(CodesBytes)));
        }

        [Test]
        public void A_request_round_trips()
        {
            var read = ChangeSecondPasswordRequest.Read(
                new PacketReader(WriteToBytes(new ChangeSecondPasswordRequest("987654321012", "210123456789"))));

            Assert.That(read.OldCode, Is.EqualTo("987654321012"));
            Assert.That(read.NewCode, Is.EqualTo("210123456789"));
        }

        [Test]
        public void A_request_with_no_new_code_throws()
        {
            Assert.That(
                () => ChangeSecondPasswordRequest.Read(new PacketReader(Hex.Bytes("0007" + "313233343536" + "00"))),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => ChangeSecondPasswordRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static byte[] WriteToBytes(ChangeSecondPasswordRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }
    }
}
