using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Messages.Inventory.Events
{
    public class ItemActionFailedEvent : IServerEvent
    {
        public ItemActionFailedEvent(ItemActionFailure failure)
        {
            Failure = failure;
        }

        public ItemActionFailure Failure { get; }
    }
}
