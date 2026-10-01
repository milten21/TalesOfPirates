using System;

namespace Top.Client.Messages.Inventory.Commands
{
    public class DeleteBagItemCommand : IServerCommand
    {
        public DeleteBagItemCommand(uint worldId, int bagSlot)
        {
            if (bagSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bagSlot), bagSlot, "A bag slot is 0 or more.");
            }

            WorldId = worldId;
            BagSlot = bagSlot;
        }

        public uint WorldId { get; }

        public int BagSlot { get; }
    }
}
