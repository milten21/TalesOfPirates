using System;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class KitbagNotice : ActionNotice
    {
        public const byte Action = 6;

        public KitbagNotice(uint worldId, uint packetId, KitbagSync kitbag)
            : base(worldId, packetId)
        {
            Kitbag = kitbag ?? throw new ArgumentNullException(nameof(kitbag));
        }

        public override byte ActionType => Action;

        public KitbagSync Kitbag { get; }

        internal static KitbagNotice ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new KitbagNotice(worldId, packetId, KitbagSync.Read(packet, KitbagSlot.DefaultBoatItemIds));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            Kitbag.Write(packet);
        }
    }
}
