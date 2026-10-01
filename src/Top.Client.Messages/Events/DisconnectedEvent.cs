namespace Top.Client.Messages.Events
{
    public class DisconnectedEvent : IServerEvent
    {
        public DisconnectedEvent(DisconnectReason reason)
        {
            Reason = reason;
        }

        public DisconnectReason Reason { get; }
    }
}
