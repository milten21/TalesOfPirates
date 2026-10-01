using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class ItemFailedNotice : ActionNotice
    {
        public const byte Action = 15;

        public ItemFailedNotice(uint worldId, uint packetId, short result)
            : base(worldId, packetId)
        {
            Result = result;
        }

        public override byte ActionType => Action;

        public short Result { get; }

        internal static ItemFailedNotice ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new ItemFailedNotice(worldId, packetId, packet.ReadShort());
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteShort(Result);
        }
    }
}
