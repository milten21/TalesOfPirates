using System;
using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Messages.Inventory.Events
{
    public class BagChangedEvent : IServerEvent
    {
        public BagChangedEvent(uint worldId, BagSync sync)
        {
            WorldId = worldId;
            Sync = sync ?? throw new ArgumentNullException(nameof(sync));
        }

        public uint WorldId { get; }

        public BagSync Sync { get; }
    }
}
