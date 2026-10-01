namespace Top.Client.Messages.Inventory.Events
{
    public class BagLockReportedEvent : IServerEvent
    {
        public BagLockReportedEvent(bool isLocked)
        {
            IsLocked = isLocked;
        }

        public bool IsLocked { get; }
    }
}
