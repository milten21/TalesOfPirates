using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class MoveNotice : ActionNotice
    {
        public const byte Action = 1;

        public MoveNotice(uint worldId, uint packetId, MoveInfo move)
            : base(worldId, packetId)
        {
            Move = move ?? throw new ArgumentNullException(nameof(move));
        }

        public override byte ActionType => Action;

        public MoveInfo Move { get; }

        internal static MoveNotice ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new MoveNotice(worldId, packetId, MoveInfo.Read(packet));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            Move.Write(packet);
        }
    }
}
