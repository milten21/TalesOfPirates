using Top.Client.Messages.Party.Data;

namespace Top.Client.Messages.Party.Events
{
    public class PartyInviteEndedEvent : IServerEvent
    {
        public PartyInviteEndedEvent(uint inviterId, PartyInviteEndReason reason)
        {
            InviterId = inviterId;
            Reason = reason;
        }

        public uint InviterId { get; }

        public PartyInviteEndReason Reason { get; }
    }
}
