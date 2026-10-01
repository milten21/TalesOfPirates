using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class MoveBagItemEncoder
    {
        public static void Encode(MoveBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemMoveRequest(command.WorldId, packetId, checked((short)command.SourceSlot),
                checked((short)command.Count), checked((short)command.DestinationSlot)).Write(packet);
        }
    }
}
