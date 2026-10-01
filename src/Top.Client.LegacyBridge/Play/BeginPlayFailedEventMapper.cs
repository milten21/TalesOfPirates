using Top.Client.Messages.Play.Events;

namespace Top.Client.LegacyBridge.Play
{
    public static class BeginPlayFailedEventMapper
    {
        public static BeginPlayFailedEvent ToBeginPlayFailedEvent(ushort code)
        {
            return new BeginPlayFailedEvent(code, ServerError.ToText(code));
        }
    }
}
