using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class DeleteBagItemEncoder
    {
        public static void Encode(DeleteBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemDeleteRequest(command.WorldId, packetId, checked((short)command.BagSlot)).Write(packet);
        }
    }
}
