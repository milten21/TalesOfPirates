using System;

namespace Top.Legacy.Protocol.Packets
{
    public readonly struct Point : IEquatable<Point>
    {
        public const int Size = 2 * sizeof(int);

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public static Point Read(byte[] raw, int at)
        {
            return new Point(ReadInt(raw, at), ReadInt(raw, at + sizeof(int)));
        }

        public void Write(byte[] raw, int at)
        {
            WriteInt(raw, at, X);
            WriteInt(raw, at + sizeof(int), Y);
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object other)
        {
            return other is Point point && Equals(point);
        }

        public override int GetHashCode()
        {
            return (X * 397) ^ Y;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        private static int ReadInt(byte[] raw, int at)
        {
            return raw[at] | (raw[at + 1] << 8) | (raw[at + 2] << 16) | (raw[at + 3] << 24);
        }

        private static void WriteInt(byte[] raw, int at, int value)
        {
            raw[at] = (byte)value;
            raw[at + 1] = (byte)(value >> 8);
            raw[at + 2] = (byte)(value >> 16);
            raw[at + 3] = (byte)(value >> 24);
        }
    }
}
