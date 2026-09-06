using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol.Tests
{
    public class FrameReaderTests
    {
        [Test]
        public void A_packet_round_trips()
        {
            var stream = new MemoryStream();
            var packet = new byte[] { 0x01, 0xAF, 0x00, 0x03, 0x41, 0x42, 0x00 };

            new FrameWriter(stream).WritePacket(packet);
            stream.Position = 0;

            Assert.That(new FrameReader(stream).ReadPacket(), Is.EqualTo(packet));
        }

        [Test]
        public void A_read_passes_over_the_idle_frames_before_a_packet()
        {
            var stream = new MemoryStream();
            var writer = new FrameWriter(stream);

            writer.WriteIdleFrame();
            writer.WriteIdleFrame();
            writer.WritePacket(new byte[] { 0x02, 0x03 });
            stream.Position = 0;

            Assert.That(new FrameReader(stream).ReadPacket(), Is.EqualTo(new byte[] { 0x02, 0x03 }));
        }

        [Test]
        public void A_four_byte_length_field_round_trips()
        {
            var frameFormat = new FrameFormat(lengthSize: 4, sessionIdSize: 0);
            var stream = new MemoryStream();

            new FrameWriter(stream, frameFormat).WritePacket(new byte[] { 0x02, 0x03 });
            stream.Position = 0;

            Assert.That(new FrameReader(stream, frameFormat).ReadPacket(), Is.EqualTo(new byte[] { 0x02, 0x03 }));
        }

        [Test]
        public void A_frame_too_short_to_hold_an_opcode_is_refused()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x06, 0x00, 0x00, 0x00, 0x00 });

            Assert.Throws<InvalidDataException>(() => new FrameReader(stream).ReadPacket());
        }

        [Test]
        public void A_frame_past_the_receive_cap_is_refused()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x20, 0x00, 0x00, 0x00, 0x00 });
            var reader = new FrameReader(stream, new FrameFormat(maxReceiveLength: 16));

            Assert.Throws<InvalidDataException>(() => reader.ReadPacket());
        }

        [Test]
        public void A_frame_that_ends_early_is_refused()
        {
            var stream = new MemoryStream(new byte[] { 0x00, 0x08, 0x00, 0x00 });

            Assert.Throws<EndOfStreamException>(() => new FrameReader(stream).ReadPacket());
        }
    }
}
