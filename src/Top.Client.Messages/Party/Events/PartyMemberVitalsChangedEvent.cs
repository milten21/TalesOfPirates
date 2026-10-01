using System;
using Top.Client.Messages.Party.Data;

namespace Top.Client.Messages.Party.Events
{
    public class PartyMemberVitalsChangedEvent : IServerEvent
    {
        public PartyMemberVitalsChangedEvent(uint memberId, PartyMemberVitals vitals)
        {
            MemberId = memberId;
            Vitals = vitals ?? throw new ArgumentNullException(nameof(vitals));
        }

        public uint MemberId { get; }

        public PartyMemberVitals Vitals { get; }
    }
}
