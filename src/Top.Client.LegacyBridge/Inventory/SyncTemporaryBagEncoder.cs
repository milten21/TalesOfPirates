using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class SyncTemporaryBagEncoder
    {
        public static void Encode(SyncTemporaryBagCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new TemporaryKitbagSyncRequest().Write(packet);
        }
    }
}
