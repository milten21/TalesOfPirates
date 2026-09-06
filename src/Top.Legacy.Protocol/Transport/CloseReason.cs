namespace Top.Legacy.Protocol.Transport
{
    public enum CloseReason
    {
        CallerClosed,
        Unreachable,
        HandshakeFailed,
        GateClosed,
        TimedOut,
        Broken,
    }
}
