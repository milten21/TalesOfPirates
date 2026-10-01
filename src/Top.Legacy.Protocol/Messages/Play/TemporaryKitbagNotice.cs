using System;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class TemporaryKitbagNotice : ActionNotice
    {
        public const byte Action = 28;

        public TemporaryKitbagNotice(uint worldId, uint packetId, KitbagSync kitbag)
            : base(worldId, packetId)
        {
            Kitbag = kitbag ?? throw new ArgumentNullException(nameof(kitbag));
        }

        public override byte ActionType => Action;

        public KitbagSync Kitbag { get; }

        internal static TemporaryKitbagNotice ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new TemporaryKitbagNotice(worldId, packetId, KitbagSync.Read(packet, KitbagSlot.DefaultBoatItemIds));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            Kitbag.Write(packet);
        }
    }
}
