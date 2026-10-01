using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class TemporaryKitbagDragRequest : BeginActionRequest
    {
        public const byte Action = 29;

        public const short FirstFreeGridId = -1;

        public TemporaryKitbagDragRequest(uint worldId, uint packetId, short sourceGridId, short count,
            short targetGridId)
            : base(worldId, packetId)
        {
            SourceGridId = sourceGridId;
            Count = count;
            TargetGridId = targetGridId;
        }

        public override byte ActionType => Action;

        public short SourceGridId { get; }

        public short Count { get; }

        public short TargetGridId { get; }

        internal static TemporaryKitbagDragRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            var sourceGridId = packet.ReadShort();
            var count = packet.ReadShort();

            return new TemporaryKitbagDragRequest(worldId, packetId, sourceGridId, count, packet.ReadShort());
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteShort(SourceGridId);
            packet.WriteShort(Count);
            packet.WriteShort(TargetGridId);
        }
    }
}
