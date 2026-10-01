using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class ChangeSecondPasswordReply
    {
        public ChangeSecondPasswordReply(ushort code)
        {
            Code = code;
        }

        public ushort Code { get; }

        public static ChangeSecondPasswordReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new ChangeSecondPasswordReply(packet.ReadUShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUShort(Code);
        }
    }
}
