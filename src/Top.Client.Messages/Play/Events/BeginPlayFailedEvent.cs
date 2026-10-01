namespace Top.Client.Messages.Play.Events
{
    public class BeginPlayFailedEvent : IServerEvent
    {
        public BeginPlayFailedEvent(int code, string text)
        {
            Code = code;
            Text = text ?? string.Empty;
        }

        public int Code { get; }

        public string Text { get; }
    }
}
