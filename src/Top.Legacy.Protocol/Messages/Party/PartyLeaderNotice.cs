using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyLeaderNotice
    {
        public PartyLeaderNotice(uint worldId, uint leaderId)
        {
            WorldId = worldId;
            LeaderId = leaderId;
        }

        public uint WorldId { get; }

        public uint LeaderId { get; }

        public static PartyLeaderNotice Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var worldId = packet.ReadUInt();

            return new PartyLeaderNotice(worldId, packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(WorldId);
            packet.WriteUInt(LeaderId);
        }
    }
}
