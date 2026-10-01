using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class Face
    {
        public Face(short angle, short pose)
        {
            Angle = angle;
            Pose = pose;
        }

        public short Angle { get; }

        public short Pose { get; }

        public static Face Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var angle = packet.ReadShort();

            return new Face(angle, packet.ReadShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteShort(Angle);
            packet.WriteShort(Pose);
        }
    }
}
