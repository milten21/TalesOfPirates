using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class ItemDeleteRequest : BeginActionRequest
    {
        public const byte Action = 13;

        public ItemDeleteRequest(uint worldId, uint packetId, short gridId)
            : base(worldId, packetId)
        {
            GridId = gridId;
        }

        public override byte ActionType => Action;

        public short GridId { get; }

        internal static ItemDeleteRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new ItemDeleteRequest(worldId, packetId, packet.ReadShort());
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteShort(GridId);
        }
    }
}
