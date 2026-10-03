using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Cli.Core;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Cli.Features.Party;

public class PartyState
{
    private IReadOnlyList<PartyMember> _members = [];

    public PartyState(ScenarioSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        session.Subscribe<LoginSucceededEvent>(_ => _members = []);
        session.Subscribe<DisconnectedEvent>(_ => _members = []);
        session.Subscribe<PartyChangedEvent>(changed => _members = changed.Members);
    }

    public IReadOnlyList<PartyMember> RequireMembersOf(CharacterState player)
    {
        return _members.Any(member => member.CharacterId == player.WorldId)
            ? _members
            : throw new StepFailedException($"{player.Name} is in no party");
    }
}
