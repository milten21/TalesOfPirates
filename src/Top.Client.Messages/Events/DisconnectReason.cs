namespace Top.Client.Messages.Events
{
    public enum DisconnectReason
    {
        ClientClosed,
        Unreachable,
        HandshakeFailed,
        ServerClosed,
        TimedOut,
        Broken,
    }
}
