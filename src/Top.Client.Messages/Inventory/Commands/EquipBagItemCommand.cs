using System;

namespace Top.Client.Messages.Inventory.Commands
{
    public class EquipBagItemCommand : IServerCommand
    {
        public EquipBagItemCommand(uint worldId, int bagSlot, bool isSecondSlot)
        {
            if (bagSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bagSlot), bagSlot, "A bag slot is 0 or more.");
            }

            WorldId = worldId;
            BagSlot = bagSlot;
            IsSecondSlot = isSecondSlot;
        }

        public uint WorldId { get; }

        public int BagSlot { get; }

        public bool IsSecondSlot { get; }
    }
}
