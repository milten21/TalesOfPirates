using System;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class LookNotice : ActionNotice
    {
        public const byte Action = 5;

        public LookNotice(uint worldId, uint packetId, LookInfo look)
            : base(worldId, packetId)
        {
            Look = look ?? throw new ArgumentNullException(nameof(look));
        }

        public override byte ActionType => Action;

        public LookInfo Look { get; }

        internal static LookNotice ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new LookNotice(worldId, packetId, LookInfo.Read(packet));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            Look.Write(packet);
        }
    }
}
