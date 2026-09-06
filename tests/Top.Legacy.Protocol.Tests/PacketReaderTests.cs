using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PacketReaderTests
    {
        [Test]
        public void Scalars_round_trip()
        {
            var writer = new PacketWriter(Opcode.BeginAction);
            writer.WriteByte(0x03);
            writer.WriteUShort(0x0405);
            writer.WriteUInt(0x06070809);

            var reader = new PacketReader(writer.ToArray());

            Assert.That(reader.ReadUShort(), Is.EqualTo(Opcode.BeginAction));
            Assert.That(reader.ReadByte(), Is.EqualTo(0x03));
            Assert.That(reader.ReadUShort(), Is.EqualTo(0x0405));
            Assert.That(reader.ReadUInt(), Is.EqualTo(0x06070809));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_gbk_string_round_trips()
        {
            var writer = new PacketWriter(Opcode.Login);
            writer.WriteString("中国 pirate");

            var reader = new PacketReader(writer.ToArray());
            reader.ReadUShort();

            Assert.That(reader.ReadString(), Is.EqualTo("中国 pirate"));
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_string_reads_without_its_terminator()
        {
            var reader = new PacketReader(new byte[] { 0x00, 0x05, 0xD6, 0xD0, 0xB9, 0xFA, 0x00 });

            Assert.That(reader.ReadString(), Is.EqualTo("中国"), "the GBK bytes of 中国 behind a count of five");
        }

        [Test]
        public void An_empty_string_round_trips()
        {
            var writer = new PacketWriter(0);
            writer.WriteString(string.Empty);

            var reader = new PacketReader(writer.ToArray());
            reader.ReadUShort();

            Assert.That(reader.ReadString(), Is.Empty);
            Assert.That(reader.Remaining, Is.Zero);
        }

        [Test]
        public void A_string_of_no_bytes_at_all_reads_as_empty()
        {
            var reader = new PacketReader(new byte[] { 0x00, 0x00 });

            Assert.That(reader.ReadString(), Is.Empty);
        }

        [Test]
        public void A_sequence_round_trips()
        {
            var writer = new PacketWriter(0);
            writer.WriteSequence(new byte[] { 0x41, 0x42, 0x43 });

            var reader = new PacketReader(writer.ToArray());
            reader.ReadUShort();

            Assert.That(reader.ReadSequence(), Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
        }

        [Test]
        public void Points_round_trip()
        {
            var points = new[] { new Point(0, 0), new Point(51200, -1), new Point(-70400, 33) };
            var writer = new PacketWriter(Opcode.BeginAction);
            writer.WritePoints(points);

            var reader = new PacketReader(writer.ToArray());
            reader.ReadUShort();

            Assert.That(reader.ReadPoints(), Is.EqualTo(points));
        }

        [Test]
        public void A_read_starts_where_the_offset_says()
        {
            var reader = new PacketReader(new byte[] { 0xFF, 0xFF, 0x01, 0xAF }, 2, 2);

            Assert.That(reader.ReadUShort(), Is.EqualTo(Opcode.Login));
        }

        [Test]
        public void A_window_that_leaves_the_packet_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PacketReader(new byte[4], 2, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PacketReader(new byte[4], -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PacketReader(new byte[4], 0, -1));
        }

        [Test]
        public void A_skip_passes_over_the_bytes_asked_for()
        {
            var reader = new PacketReader(new byte[] { 0x00, 0x00, 0xFF, 0x01, 0xAF });
            reader.ReadUShort();

            reader.Skip(1);

            Assert.That(reader.ReadUShort(), Is.EqualTo(Opcode.Login));
        }

        [Test]
        public void A_read_past_the_end_is_refused()
        {
            var reader = new PacketReader(new byte[] { 0x00 });

            Assert.Throws<InvalidDataException>(() => reader.ReadUShort());
        }

        [Test]
        public void A_point_sequence_that_is_not_whole_points_is_refused()
        {
            var reader = new PacketReader(new byte[] { 0x00, 0x03, 0x01, 0x02, 0x03 });

            Assert.Throws<InvalidDataException>(() => reader.ReadPoints());
        }
    }
}
