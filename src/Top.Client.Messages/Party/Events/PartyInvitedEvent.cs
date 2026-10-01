using System;

namespace Top.Client.Messages.Party.Events
{
    public class PartyInvitedEvent : IServerEvent
    {
        public PartyInvitedEvent(uint inviterId, string inviterName)
        {
            InviterId = inviterId;
            InviterName = inviterName ?? throw new ArgumentNullException(nameof(inviterName));
        }

        public uint InviterId { get; }

        public string InviterName { get; }
    }
}
