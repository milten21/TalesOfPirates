using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class UnequipItemEncoder
    {
        public static void Encode(UnequipItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemUnfixRequest(command.WorldId, packetId, checked((byte)command.Slot),
                checked((short)command.BagSlot)).Write(packet);
        }
    }
}
