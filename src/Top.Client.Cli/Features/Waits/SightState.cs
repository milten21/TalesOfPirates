using System;
using System.Collections.Generic;
using Top.Client.Cli.Core;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Features.Waits;

public class SightState
{
    private readonly Dictionary<uint, string> _actorNames = [];

    public SightState(ScenarioSession session)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        session.Subscribe<LoginSucceededEvent>(_ => _actorNames.Clear());
        session.Subscribe<MapEnteredEvent>(_ => _actorNames.Clear());
        session.Subscribe<DisconnectedEvent>(_ => _actorNames.Clear());
        session.Subscribe<CharacterAppearedEvent>(appeared =>
            _actorNames[appeared.Character.WorldId] = appeared.Character.Name);
        session.Subscribe<CharacterDisappearedEvent>(disappeared => _actorNames.Remove(disappeared.WorldId));
    }

    public bool IsInSight(string actorName)
    {
        return _actorNames.ContainsValue(actorName);
    }
}
