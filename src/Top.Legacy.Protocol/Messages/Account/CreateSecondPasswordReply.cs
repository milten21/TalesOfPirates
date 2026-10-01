using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class CreateSecondPasswordReply
    {
        public CreateSecondPasswordReply(ushort code)
        {
            Code = code;
        }

        public ushort Code { get; }

        public static CreateSecondPasswordReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new CreateSecondPasswordReply(packet.ReadUShort());
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
