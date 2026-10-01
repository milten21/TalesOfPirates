using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages.Party.Events;

namespace Top.Client.Features.Party;

public class PartyRoster(uint playerId)
{
    private List<PartyRosterMember> _members = [];

    private uint? _leaderId;

    public IReadOnlyList<PartyRosterMember> Members => _members;

    public bool IsPlayerLeader => _leaderId == playerId;

    public void ChangeMembers(PartyChangedEvent changed)
    {
        if (changed.Members.All(member => member.CharacterId != playerId))
        {
            _members = [];
            _leaderId = null;

            return;
        }

        var leaderId = changed.Members[0].CharacterId;

        _members = changed.Members
            .Where(member => member.CharacterId != playerId)
            .Select(member => new PartyRosterMember(member.CharacterId, member.Name,
                member.CharacterId == leaderId, FindMember(member.CharacterId)?.Vitals))
            .ToList();
        _leaderId = leaderId;
    }

    public void ChangeVitals(PartyMemberVitalsChangedEvent changed)
    {
        var index = _members.FindIndex(member => member.CharacterId == changed.MemberId);

        if (index >= 0)
        {
            _members[index] = _members[index] with { Vitals = changed.Vitals };
        }
    }

    private PartyRosterMember? FindMember(uint characterId)
    {
        return _members.Find(member => member.CharacterId == characterId);
    }
}
