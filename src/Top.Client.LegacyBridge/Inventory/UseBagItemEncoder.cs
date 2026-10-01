using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class UseBagItemEncoder
    {
        public static void Encode(UseBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemUseRequest(command.WorldId, packetId, checked((short)command.BagSlot), ItemUseRequest.NoTargetItem)
                .Write(packet);
        }
    }
}
