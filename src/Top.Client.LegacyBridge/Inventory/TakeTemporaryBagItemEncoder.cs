using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class TakeTemporaryBagItemEncoder
    {
        public static void Encode(TakeTemporaryBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var targetGridId = command.BagSlot is { } bagSlot
                ? checked((short)bagSlot)
                : TemporaryKitbagDragRequest.FirstFreeGridId;

            new TemporaryKitbagDragRequest(command.WorldId, packetId, checked((short)command.TemporarySlot),
                checked((short)command.Count), targetGridId).Write(packet);
        }
    }
}
