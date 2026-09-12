using System;
using System.IO;
using Top.Legacy.Text;

namespace Top.Legacy.Protocol.Packets
{
    public class PacketReader
    {
        private readonly byte[] _packet;
        private readonly int _end;
        private int _at;

        public PacketReader(byte[] packet) : this(packet, 0, packet?.Length ?? 0)
        {
        }

        public PacketReader(byte[] packet, int offset, int count)
        {
            _packet = packet ?? throw new ArgumentNullException(nameof(packet));

            if (offset < 0 || count < 0 || count > _packet.Length - offset)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    $"A window of {count} bytes at {offset} is outside a packet of {_packet.Length} bytes.");
            }

            _at = offset;
            _end = offset + count;
        }

        public int Remaining => _end - _at;

        public byte ReadByte()
        {
            Require(sizeof(byte));

            return _packet[_at++];
        }

        public ushort ReadUShort()
        {
            var value = PeekUShort();
            _at += sizeof(ushort);

            return value;
        }

        public ushort PeekUShort()
        {
            Require(sizeof(ushort));

            return (ushort)((_packet[_at] << 8) | _packet[_at + 1]);
        }

        public uint ReadUInt()
        {
            Require(sizeof(uint));
            var value = ((uint)_packet[_at] << 24)
                        | ((uint)_packet[_at + 1] << 16)
                        | ((uint)_packet[_at + 2] << 8)
                        | _packet[_at + 3];
            _at += sizeof(uint);

            return value;
        }

        public string ReadString()
        {
            var count = ReadUShort();

            if (count == 0)
            {
                return string.Empty;
            }

            Require(count);
            var text = Gbk.GetString(_packet, _at, count - 1);
            _at += count;

            return text;
        }

        public byte[] ReadSequence()
        {
            var count = ReadUShort();
            Require(count);
            var raw = new byte[count];
            Array.Copy(_packet, _at, raw, 0, count);
            _at += count;

            return raw;
        }

        public Point[] ReadPoints()
        {
            var raw = ReadSequence();

            if (raw.Length % Point.Size != 0)
            {
                throw new InvalidDataException($"A point sequence of {raw.Length} bytes is not whole points.");
            }

            var points = new Point[raw.Length / Point.Size];

            for (var i = 0; i < points.Length; i++)
            {
                points[i] = Point.Read(raw, i * Point.Size);
            }

            return points;
        }

        public void Skip(int count)
        {
            Require(count);
            _at += count;
        }

        private void Require(int count)
        {
            if (count < 0 || Remaining < count)
            {
                throw new InvalidDataException($"The packet has {Remaining} bytes left and {count} were asked for.");
            }
        }
    }
}
