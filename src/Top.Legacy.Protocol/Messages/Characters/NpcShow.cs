using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class NpcShow
    {
        public NpcShow(byte npcType, byte npcState)
        {
            NpcType = npcType;
            NpcState = npcState;
        }

        public byte NpcType { get; }

        public byte NpcState { get; }

        public static NpcShow Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var npcType = packet.ReadByte();

            return new NpcShow(npcType, packet.ReadByte());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(NpcType);
            packet.WriteByte(NpcState);
        }
    }
}
