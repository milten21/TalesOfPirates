using System;
using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Messages.Inventory.Commands
{
    public class UnequipItemCommand : IServerCommand
    {
        public UnequipItemCommand(uint worldId, EquipmentSlot slot, int bagSlot)
        {
            if (bagSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bagSlot), bagSlot, "A bag slot is 0 or more.");
            }

            WorldId = worldId;
            Slot = slot;
            BagSlot = bagSlot;
        }

        public uint WorldId { get; }

        public EquipmentSlot Slot { get; }

        public int BagSlot { get; }
    }
}
