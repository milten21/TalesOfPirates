using Top.Client.Messages;

namespace Top.Client.LegacyBridge
{
    internal readonly struct QueuedEvent
    {
        public QueuedEvent(int generation, IServerEvent serverEvent)
        {
            Generation = generation;
            Event = serverEvent;
        }

        public int Generation { get; }

        public IServerEvent Event { get; }
    }
}
