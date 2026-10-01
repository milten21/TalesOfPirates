namespace Top.Client.Messages.Play.Events
{
    public class PreMoveTimeChangedEvent : IServerEvent
    {
        public PreMoveTimeChangedEvent(int milliseconds)
        {
            Milliseconds = milliseconds;
        }

        public int Milliseconds { get; }
    }
}
