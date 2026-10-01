using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class EquipBagItemEncoder
    {
        public static void Encode(EquipBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemUseRequest(command.WorldId, packetId, checked((short)command.BagSlot),
                command.IsSecondSlot ? ItemUseRequest.SecondLink : ItemUseRequest.FirstFreeLink).Write(packet);
        }
    }
}
