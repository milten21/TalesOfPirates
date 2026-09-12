using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol.Tests
{
    public class FrameWriterTests
    {
        [Test]
        public void A_frame_opens_with_its_total_length_and_the_session_id()
        {
            var stream = new MemoryStream();

            new FrameWriter(stream).WritePacket(new byte[] { 0x01, 0xAF });

            Assert.That(
                stream.ToArray(),
                Is.EqualTo(new byte[] { 0x00, 0x08, 0x80, 0x00, 0x00, 0x00, 0x01, 0xAF }),
                "a length of eight over the two length bytes, the four session id bytes and the packet, "
                + "and a session id the gate reads as a plain packet that wants no reply");
        }

        [Test]
        public void The_idle_frame_is_the_length_field_alone()
        {
            var stream = new MemoryStream();

            new FrameWriter(stream).WriteIdleFrame();

            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x02 }));
        }

        [Test]
        public void A_four_byte_length_field_spans_four_bytes()
        {
            var stream = new MemoryStream();

            new FrameWriter(stream, new FrameFormat(lengthSize: 4, sessionIdSize: 0))
                .WritePacket(new byte[] { 0x02, 0x03 });

            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x06, 0x02, 0x03 }));
        }

        [Test]
        public void A_packet_past_the_send_cap_is_refused()
        {
            var writer = new FrameWriter(new MemoryStream(), new FrameFormat(maxSendLength: 16));

            Assert.Throws<InvalidDataException>(() => writer.WritePacket(new byte[16]));
        }
    }
}
