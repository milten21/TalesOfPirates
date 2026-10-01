using System;
using Top.Client.Messages;
using Top.Client.Messages.Inventory.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class TemporaryBagSyncDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new TemporaryBagChangedEvent(BagSyncMapper.ToBagSync(TemporaryKitbagSyncReply.Read(packet).Kitbag));
        }
    }
}
