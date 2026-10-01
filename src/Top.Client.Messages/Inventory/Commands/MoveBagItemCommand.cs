using System;

namespace Top.Client.Messages.Inventory.Commands
{
    public class MoveBagItemCommand : IServerCommand
    {
        public MoveBagItemCommand(uint worldId, int sourceSlot, int count, int destinationSlot)
        {
            if (sourceSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceSlot), sourceSlot, "A bag slot is 0 or more.");
            }

            if (destinationSlot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationSlot), destinationSlot,
                    "A bag slot is 0 or more.");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "A move takes at least one item.");
            }

            WorldId = worldId;
            SourceSlot = sourceSlot;
            Count = count;
            DestinationSlot = destinationSlot;
        }

        public uint WorldId { get; }

        public int SourceSlot { get; }

        public int Count { get; }

        public int DestinationSlot { get; }
    }
}
