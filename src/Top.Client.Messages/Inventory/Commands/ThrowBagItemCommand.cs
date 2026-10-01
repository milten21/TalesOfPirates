using System;
using System.Numerics;

namespace Top.Client.Messages.Inventory.Commands
{
    public class ThrowBagItemCommand : IServerCommand
    {
        public ThrowBagItemCommand(uint worldId, int bagSlot, int count, Vector2 dropPosition)
        {
            if (bagSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bagSlot), bagSlot, "A bag slot is 0 or more.");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A throw takes at least one item.");
            }

            WorldId = worldId;
            BagSlot = bagSlot;
            Count = count;
            DropPosition = dropPosition;
        }

        public uint WorldId { get; }

        public int BagSlot { get; }

        public int Count { get; }

        public Vector2 DropPosition { get; }
    }
}
