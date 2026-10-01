using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class ItemUseRequest : BeginActionRequest
    {
        public const byte Action = 11;

        public const short FirstFreeLink = -1;

        public const short SecondLink = -2;

        public const short NoTargetItem = -1;

        public ItemUseRequest(uint worldId, uint packetId, short gridId, short targetGridId)
            : base(worldId, packetId)
        {
            GridId = gridId;
            TargetGridId = targetGridId;
        }

        public override byte ActionType => Action;

        public short GridId { get; }

        public short TargetGridId { get; }

        internal static ItemUseRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            var gridId = packet.ReadShort();

            return new ItemUseRequest(worldId, packetId, gridId, packet.ReadShort());
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteShort(GridId);
            packet.WriteShort(TargetGridId);
        }
    }
}
