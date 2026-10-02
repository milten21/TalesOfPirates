namespace Top.Client.LegacyBridge
{
    public interface IPacketObserver
    {
        void Observe(ObservedPacket packet);
    }
}
