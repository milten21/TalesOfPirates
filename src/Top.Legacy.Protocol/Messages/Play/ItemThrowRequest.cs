using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class ItemThrowRequest : BeginActionRequest
    {
        public const byte Action = 9;

        public ItemThrowRequest(uint worldId, uint packetId, short gridId, short count, Point dropPosition)
            : base(worldId, packetId)
        {
            GridId = gridId;
            Count = count;
            DropPosition = dropPosition;
        }

        public override byte ActionType => Action;

        public short GridId { get; }

        public short Count { get; }

        public Point DropPosition { get; }

        internal static ItemThrowRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            var gridId = packet.ReadShort();
            var count = packet.ReadShort();
            var x = packet.ReadInt();

            return new ItemThrowRequest(worldId, packetId, gridId, count, new Point(x, packet.ReadInt()));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteShort(GridId);
            packet.WriteShort(Count);
            packet.WriteInt(DropPosition.X);
            packet.WriteInt(DropPosition.Y);
        }
    }
}
