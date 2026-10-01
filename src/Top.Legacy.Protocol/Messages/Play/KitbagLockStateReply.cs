using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class KitbagLockStateReply
    {
        public KitbagLockStateReply(bool isLocked)
        {
            IsLocked = isLocked;
        }

        public bool IsLocked { get; }

        public static KitbagLockStateReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new KitbagLockStateReply(packet.ReadByte() != 0);
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(IsLocked ? (byte)1 : (byte)0);
        }
    }
}
