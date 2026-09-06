using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Text;

namespace Top.Legacy.Protocol.Packets
{
    public class PacketWriter
    {
        private readonly List<byte> _packet = new List<byte>();

        public PacketWriter()
        {
        }

        public PacketWriter(ushort opcode)
        {
            WriteUShort(opcode);
        }

        public void WriteByte(byte value)
        {
            _packet.Add(value);
        }

        public void WriteUShort(ushort value)
        {
            _packet.Add((byte)(value >> 8));
            _packet.Add((byte)value);
        }

        public void WriteUInt(uint value)
        {
            _packet.Add((byte)(value >> 24));
            _packet.Add((byte)(value >> 16));
            _packet.Add((byte)(value >> 8));
            _packet.Add((byte)value);
        }

        public void WriteString(string text)
        {
            var bytes = Gbk.GetBytes(text ?? string.Empty);
            WriteCount(bytes.Length + 1);
            _packet.AddRange(bytes);
            _packet.Add(0);
        }

        public void WriteSequence(byte[] raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            WriteCount(raw.Length);
            _packet.AddRange(raw);
        }

        public void WriteRaw(byte[] raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            _packet.AddRange(raw);
        }

        public void WritePoints(Point[] points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            var raw = new byte[points.Length * Point.Size];

            for (var i = 0; i < points.Length; i++)
            {
                points[i].Write(raw, i * Point.Size);
            }

            WriteSequence(raw);
        }

        public byte[] ToArray()
        {
            return _packet.ToArray();
        }

        private void WriteCount(int count)
        {
            if (count > ushort.MaxValue)
            {
                throw new InvalidDataException($"A sequence of {count} bytes does not fit a two-byte count.");
            }

            WriteUShort((ushort)count);
        }
    }
}
