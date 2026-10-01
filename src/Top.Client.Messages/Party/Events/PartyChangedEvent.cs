using System;
using System.Collections.Generic;
using Top.Client.Messages.Party.Data;

namespace Top.Client.Messages.Party.Events
{
    public class PartyChangedEvent : IServerEvent
    {
        public PartyChangedEvent(PartyChangeKind kind, string changedMemberName, IReadOnlyList<PartyMember> members)
        {
            Kind = kind;
            ChangedMemberName = changedMemberName ?? throw new ArgumentNullException(nameof(changedMemberName));
            Members = members ?? throw new ArgumentNullException(nameof(members));
        }

        public PartyChangeKind Kind { get; }

        public string ChangedMemberName { get; }

        public IReadOnlyList<PartyMember> Members { get; }
    }
}
