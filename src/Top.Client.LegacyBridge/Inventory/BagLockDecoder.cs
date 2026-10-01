using System;
using Top.Client.Messages;
using Top.Client.Messages.Inventory.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class BagLockDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new BagLockReportedEvent(KitbagLockStateReply.Read(packet).IsLocked);
        }
    }
}
