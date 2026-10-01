using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class DeleteCharacterReply
    {
        public DeleteCharacterReply(ushort code)
        {
            Code = code;
        }

        public ushort Code { get; }

        public static DeleteCharacterReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new DeleteCharacterReply(packet.ReadUShort());
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
