using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class BeginPlayReply
    {
        public BeginPlayReply(ushort code)
        {
            Code = code;
        }

        public ushort Code { get; }

        public static BeginPlayReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new BeginPlayReply(packet.ReadUShort());
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
