using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class FailedActionNotice
    {
        public FailedActionNotice(uint worldId, byte actionType, byte reason)
        {
            WorldId = worldId;
            ActionType = actionType;
            Reason = reason;
        }

        public uint WorldId { get; }

        public byte ActionType { get; }

        public byte Reason { get; }

        public static FailedActionNotice Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var worldId = packet.ReadUInt();
            var actionType = packet.ReadByte();

            return new FailedActionNotice(worldId, actionType, packet.ReadByte());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(WorldId);
            packet.WriteByte(ActionType);
            packet.WriteByte(Reason);
        }
    }
}
