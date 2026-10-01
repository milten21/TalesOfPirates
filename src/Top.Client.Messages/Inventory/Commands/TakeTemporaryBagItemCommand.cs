using System;

namespace Top.Client.Messages.Inventory.Commands
{
    public class TakeTemporaryBagItemCommand : IServerCommand
    {
        public TakeTemporaryBagItemCommand(uint worldId, int temporarySlot, int count, int? bagSlot)
        {
            if (temporarySlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(temporarySlot), temporarySlot,
                    "A temporary bag slot is 0 or more.");
            }

            if (bagSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bagSlot), bagSlot, "A bag slot is 0 or more.");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A take moves at least one item.");
            }

            WorldId = worldId;
            TemporarySlot = temporarySlot;
            Count = count;
            BagSlot = bagSlot;
        }

        public uint WorldId { get; }

        public int TemporarySlot { get; }

        public int Count { get; }

        public int? BagSlot { get; }
    }
}
