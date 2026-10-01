using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class LockBagEncoder
    {
        public static void Encode(LockBagCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new KitbagLockRequest().Write(packet);
        }
    }
}
