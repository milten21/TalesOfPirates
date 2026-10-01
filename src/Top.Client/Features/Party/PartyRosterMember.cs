using Top.Client.Messages.Party.Data;

namespace Top.Client.Features.Party;

public record PartyRosterMember(uint CharacterId, string Name, bool IsLeader, PartyMemberVitals? Vitals);
