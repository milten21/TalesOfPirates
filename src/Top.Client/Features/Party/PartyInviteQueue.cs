using System;
using System.Collections.Generic;
using System.Linq;

namespace Top.Client.Features.Party;

public class PartyInviteQueue
{
    // TODO: a server-declared value
    public const int InviteSeconds = 30;

    public const int MaxShownInvites = 2;

    private readonly List<PendingInvite> _invites = [];

    public IReadOnlyList<PendingInvite> Invites => _invites.Take(MaxShownInvites).ToList();

    public void AddInvite(uint inviterId, string name, DateTime receivedAt)
    {
        if (_invites.Any(invite => invite.InviterId == inviterId))
        {
            return;
        }

        _invites.Add(new PendingInvite(inviterId, name, receivedAt));
    }

    public void RemoveInvite(uint inviterId)
    {
        _invites.RemoveAll(invite => invite.InviterId == inviterId);
    }
}
