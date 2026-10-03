using System;
using Top.Client.Cli.Core;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Party.Events;

namespace Top.Client.Cli.Features.Party;

public class PartyInviteState
{
    private readonly ScenarioSession _session;

    private PartyInvitedEvent? _invite;

    public PartyInviteState(ScenarioSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));

        _session.Subscribe<LoginSucceededEvent>(_ => _invite = null);
        _session.Subscribe<DisconnectedEvent>(_ => _invite = null);
        _session.Subscribe<PartyInvitedEvent>(invited => _invite = invited);
        _session.Subscribe<PartyInviteEndedEvent>(ended =>
        {
            if (ended.InviterId == _invite?.InviterId)
            {
                _invite = null;
            }
        });
    }

    public PartyInvitedEvent Take()
    {
        var invite = _invite ?? throw new StepFailedException("no party invite is open");

        _session.Events.RemoveThrough(invite, serverEvent => serverEvent is PartyInvitedEvent);
        _invite = null;

        return invite;
    }
}
