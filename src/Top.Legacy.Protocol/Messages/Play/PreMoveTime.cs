using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class PreMoveTime
    {
        public PreMoveTime(uint milliseconds)
        {
            Milliseconds = milliseconds;
        }

        public uint Milliseconds { get; }

        public static PreMoveTime Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new PreMoveTime(packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(Milliseconds);
        }
    }
}
