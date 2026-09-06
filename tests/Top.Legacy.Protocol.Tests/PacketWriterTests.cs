using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PacketWriterTests
    {
        [Test]
        public void A_packet_opens_with_its_opcode()
        {
            var writer = new PacketWriter(Opcode.Login);

            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x01, 0xAF }), "431 big-endian");
        }

        [Test]
        public void A_packet_built_without_an_opcode_opens_with_its_first_field()
        {
            var writer = new PacketWriter();

            writer.WriteUShort(0x0405);

            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x04, 0x05 }));
        }

        [Test]
        public void Raw_bytes_are_written_without_a_count()
        {
            var writer = new PacketWriter(0);

            writer.WriteRaw(new byte[] { 0x41, 0x42, 0x43 });

            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x00, 0x41, 0x42, 0x43 }));
        }

        [Test]
        public void Scalars_are_big_endian()
        {
            var writer = new PacketWriter(0x0102);

            writer.WriteByte(0x03);
            writer.WriteUShort(0x0405);
            writer.WriteUInt(0x06070809);

            Assert.That(
                writer.ToArray(),
                Is.EqualTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }));
        }

        [Test]
        public void A_sequence_sits_behind_a_two_byte_count()
        {
            var writer = new PacketWriter(0);

            writer.WriteSequence(new byte[] { 0x41, 0x42, 0x43 });

            Assert.That(
                writer.ToArray(),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x03, 0x41, 0x42, 0x43 }),
                "the opcode 0, then a count of three over the bytes");
        }

        [Test]
        public void A_string_is_gbk_bytes_behind_a_count_that_takes_in_the_terminator()
        {
            var writer = new PacketWriter(0);

            writer.WriteString("中国");

            Assert.That(
                writer.ToArray(),
                Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x05, 0xD6, 0xD0, 0xB9, 0xFA, 0x00 }),
                "the opcode 0, then a count of five over the GBK bytes of 中国 and the terminator");
        }

        [Test]
        public void An_empty_string_is_the_terminator_alone()
        {
            var writer = new PacketWriter(0);

            writer.WriteString(string.Empty);

            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }));
        }

        [Test]
        public void A_null_string_writes_as_an_empty_one()
        {
            var writer = new PacketWriter(0);

            writer.WriteString(null);

            Assert.That(writer.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x00, 0x00, 0x01, 0x00 }));
        }

        [Test]
        public void Points_are_little_endian_inside_the_sequence()
        {
            var writer = new PacketWriter(0);

            writer.WritePoints(new[] { new Point(1, -2) });

            Assert.That(
                writer.ToArray(),
                Is.EqualTo(new byte[]
                {
                    0x00, 0x00,
                    0x00, 0x08,
                    0x01, 0x00, 0x00, 0x00,
                    0xFE, 0xFF, 0xFF, 0xFF,
                }),
                "the opcode 0, a count of eight, then 1 and -2 as little-endian four-byte values");
        }

        [Test]
        public void A_sequence_that_does_not_fit_a_two_byte_count_is_refused()
        {
            var writer = new PacketWriter(0);

            Assert.Throws<InvalidDataException>(() => writer.WriteSequence(new byte[ushort.MaxValue + 1]));
        }
    }
}
