using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class UnreadActionRequest : BeginActionRequest
    {
        public UnreadActionRequest(uint worldId, uint packetId, byte actionType)
            : base(worldId, packetId)
        {
            ActionType = actionType;
        }

        public override byte ActionType { get; }

        protected override void WriteBody(PacketWriter packet)
        {
        }
    }
}
