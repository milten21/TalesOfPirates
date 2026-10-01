using System;
using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Messages.Inventory.Events
{
    public class TemporaryBagChangedEvent : IServerEvent
    {
        public TemporaryBagChangedEvent(BagSync sync)
        {
            Sync = sync ?? throw new ArgumentNullException(nameof(sync));
        }

        public BagSync Sync { get; }
    }
}
