using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class CharacterEndSee
    {
        public CharacterEndSee(byte seeType, uint worldId)
        {
            SeeType = seeType;
            WorldId = worldId;
        }

        public byte SeeType { get; }

        public uint WorldId { get; }

        public static CharacterEndSee Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var seeType = packet.ReadByte();

            return new CharacterEndSee(seeType, packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(SeeType);
            packet.WriteUInt(WorldId);
        }
    }
}
