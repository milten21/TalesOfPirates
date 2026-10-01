namespace Top.Client.Messages.Account.Events
{
    public class LoginFailedEvent : IServerEvent
    {
        public LoginFailedEvent(int code, string text)
        {
            Code = code;
            Text = text ?? string.Empty;
        }

        public int Code { get; }

        public string Text { get; }
    }
}
