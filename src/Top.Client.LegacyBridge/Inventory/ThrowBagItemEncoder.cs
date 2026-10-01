using System;
using Top.Client.Messages.Inventory.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class ThrowBagItemEncoder
    {
        public static void Encode(ThrowBagItemCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ItemThrowRequest(command.WorldId, packetId, checked((short)command.BagSlot),
                checked((short)command.Count), MapPointMapper.ToPacketPoint(command.DropPosition)).Write(packet);
        }
    }
}
