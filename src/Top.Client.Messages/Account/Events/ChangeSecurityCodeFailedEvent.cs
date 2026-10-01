namespace Top.Client.Messages.Account.Events
{
    public class ChangeSecurityCodeFailedEvent : IServerEvent
    {
        public ChangeSecurityCodeFailedEvent(int code, string text)
        {
            Code = code;
            Text = text ?? string.Empty;
        }

        public int Code { get; }

        public string Text { get; }
    }
}
