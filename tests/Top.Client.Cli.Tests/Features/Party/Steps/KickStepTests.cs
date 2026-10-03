using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Party.Steps;

public class KickStepTests : ScenarioTest
{
    private const uint MemberId = 12;

    [Test]
    public void Kick_sends_the_world_id_of_the_named_party_member()
    {
        EnterWorldWith(Joined(new PartyMember(PlayerId, "Observer"), new PartyMember(MemberId, "T1")));

        var exitCode = Run("play Observer", "kick T1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<KickPartyMemberCommand>().Single().MemberId, Is.EqualTo(MemberId));
        Assert.That(Output.ToString(), Does.Contain("line 2: kick T1: done, kicked T1 from the party"));
    }

    [Test]
    public void Kick_fails_when_the_character_is_in_no_party()
    {
        EnterWorldWith();

        var exitCode = Run("play Observer", "kick T1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: kick T1: failed, Observer is in no party"));
        Assert.That(Connection.Sent.OfType<KickPartyMemberCommand>(), Is.Empty);
    }

    [Test]
    public void Kick_fails_after_the_party_ended()
    {
        EnterWorldWith(Joined(new PartyMember(PlayerId, "Observer"), new PartyMember(MemberId, "T1")),
            new PartyChangedEvent(PartyChangeKind.Left, "T1", []));

        var exitCode = Run("play Observer", "kick T1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: kick T1: failed, Observer is in no party"));
    }

    [Test]
    public void Kick_fails_when_the_party_has_no_other_member_with_the_name()
    {
        EnterWorldWith(Joined(new PartyMember(PlayerId, "Observer"), new PartyMember(MemberId, "T1")));

        var exitCode = Run("play Observer", "kick Observer");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: kick Observer: failed, the party has no other member named Observer, only: T1"));
        Assert.That(Connection.Sent.OfType<KickPartyMemberCommand>(), Is.Empty);
    }

    [Test]
    public void Kick_fails_when_another_member_leads_the_party()
    {
        EnterWorldWith(Joined(new PartyMember(MemberId, "T1"), new PartyMember(PlayerId, "Observer"),
            new PartyMember(13, "T2")));

        var exitCode = Run("play Observer", "kick T2");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: kick T2: failed, only the party leader T1 can kick"));
        Assert.That(Connection.Sent.OfType<KickPartyMemberCommand>(), Is.Empty);
    }

    private void EnterWorldWith(params IServerEvent[] events)
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ => [CreateEnteredMap("Observer", Vector2.Zero), .. events]);
    }

    private static PartyChangedEvent Joined(params PartyMember[] members)
    {
        return new PartyChangedEvent(PartyChangeKind.Joined, members[^1].Name, members);
    }
}
