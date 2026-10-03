using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Chat;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class SayRequestTests
    {
        private const string Text = "&make 1,1";

        private const string RequestBytes = "000A" + "266D616B6520312C31" + "00";

        [Test]
        public void A_request_reads_the_text()
        {
            var request = SayRequest.Read(new PacketReader(Hex.Bytes(RequestBytes)));

            Assert.That(request.Text, Is.EqualTo(Text));
        }

        [Test]
        public void A_request_is_the_text_as_a_string_and_nothing_else()
        {
            var packet = new PacketWriter();
            new SayRequest(Text).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(RequestBytes)));
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => SayRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
