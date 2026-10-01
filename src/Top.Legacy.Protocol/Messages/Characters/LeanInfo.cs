using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class LeanInfo
    {
        public LeanInfo(byte state, int pose, int angle, Point position, int height)
        {
            State = state;
            Pose = pose;
            Angle = angle;
            Position = position;
            Height = height;
        }

        public byte State { get; }

        public int Pose { get; }

        public int Angle { get; }

        public Point Position { get; }

        public int Height { get; }

        public static LeanInfo Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var state = packet.ReadByte();
            var pose = packet.ReadInt();
            var angle = packet.ReadInt();
            var x = packet.ReadInt();
            var y = packet.ReadInt();

            return new LeanInfo(state, pose, angle, new Point(x, y), packet.ReadInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(State);
            packet.WriteInt(Pose);
            packet.WriteInt(Angle);
            packet.WriteInt(Position.X);
            packet.WriteInt(Position.Y);
            packet.WriteInt(Height);
        }
    }
}
