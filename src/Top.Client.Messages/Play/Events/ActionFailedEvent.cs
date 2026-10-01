using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class ActionFailedEvent : IServerEvent
    {
        public ActionFailedEvent(uint worldId, int actionType, ActionFailureReason reason)
        {
            WorldId = worldId;
            ActionType = actionType;
            Reason = reason;
        }

        public uint WorldId { get; }

        public int ActionType { get; }

        public ActionFailureReason Reason { get; }
    }
}
