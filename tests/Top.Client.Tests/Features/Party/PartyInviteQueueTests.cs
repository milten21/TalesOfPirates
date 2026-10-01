using System;
using System.Linq;
using NUnit.Framework;
using Top.Client.Features.Party;

namespace Top.Client.Tests.Features.Party;

public class PartyInviteQueueTests
{
    private static readonly DateTime Monday = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Tuesday = Monday.AddDays(1);

    private static readonly DateTime Wednesday = Monday.AddDays(2);

    private PartyInviteQueue _queue;

    [SetUp]
    public void CreateQueue()
    {
        _queue = new PartyInviteQueue();
    }

    [Test]
    public void A_new_invite_shows_at_once()
    {
        _queue.AddInvite(1, "Anne", Monday);

        Assert.That(_queue.Invites, Is.EqualTo(new[] { new PendingInvite(1, "Anne", Monday) }));
    }

    [Test]
    public void A_repeat_invite_from_the_same_inviter_is_ignored()
    {
        _queue.AddInvite(1, "Anne", Monday);
        _queue.AddInvite(1, "Anne", Tuesday);

        Assert.That(_queue.Invites, Is.EqualTo(new[] { new PendingInvite(1, "Anne", Monday) }));
    }

    [Test]
    public void At_most_two_invites_show_and_the_rest_wait()
    {
        _queue.AddInvite(1, "Anne", Monday);
        _queue.AddInvite(2, "Blackbeard", Tuesday);
        _queue.AddInvite(3, "Gorn", Wednesday);

        Assert.That(_queue.Invites.Select(invite => invite.InviterId), Is.EqualTo(new uint[] { 1, 2 }));
    }

    [Test]
    public void Removing_a_shown_invite_lets_the_next_waiting_invite_show()
    {
        _queue.AddInvite(1, "Anne", Monday);
        _queue.AddInvite(2, "Blackbeard", Tuesday);
        _queue.AddInvite(3, "Gorn", Wednesday);

        _queue.RemoveInvite(1);

        Assert.That(_queue.Invites.Select(invite => invite.InviterId), Is.EqualTo(new uint[] { 2, 3 }));
    }

    [Test]
    public void An_ended_event_removes_a_waiting_invite_too()
    {
        _queue.AddInvite(1, "Anne", Monday);
        _queue.AddInvite(2, "Blackbeard", Tuesday);
        _queue.AddInvite(3, "Gorn", Wednesday);

        _queue.RemoveInvite(3);
        _queue.RemoveInvite(1);

        Assert.That(_queue.Invites.Select(invite => invite.InviterId), Is.EqualTo(new uint[] { 2 }));
    }

    [Test]
    public void Accepting_or_declining_removes_the_invite_at_once()
    {
        _queue.AddInvite(1, "Anne", Monday);

        _queue.RemoveInvite(1);

        Assert.That(_queue.Invites, Is.Empty);
    }

    [Test]
    public void Removing_an_unknown_inviter_does_nothing()
    {
        _queue.AddInvite(1, "Anne", Monday);

        _queue.RemoveInvite(99);

        Assert.That(_queue.Invites.Select(invite => invite.InviterId), Is.EqualTo(new uint[] { 1 }));
    }
}
