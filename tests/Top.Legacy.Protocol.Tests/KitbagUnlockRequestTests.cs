using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class KitbagUnlockRequestTests
    {
        private const string Code = "123456";
        private const string CodeBytes = "0007" + "313233343536" + "00";

        [Test]
        public void A_request_reads_the_code()
        {
            Assert.That(KitbagUnlockRequest.Read(new PacketReader(Hex.Bytes(CodeBytes))).Code, Is.EqualTo(Code));
        }

        [Test]
        public void A_request_is_one_string_carrying_the_code_as_plain_text()
        {
            var packet = new PacketWriter();
            new KitbagUnlockRequest(Code).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(CodeBytes)));
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(() => KitbagUnlockRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
