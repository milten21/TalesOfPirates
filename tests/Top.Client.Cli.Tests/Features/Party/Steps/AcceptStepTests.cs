using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Party.Steps;

public class AcceptStepTests : ScenarioTest
{
    [Test]
    public void Accept_answers_the_open_party_invite()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1")]);

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 1", "accept");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<AcceptInviteCommand>().Single().InviterId, Is.EqualTo(7));
        Assert.That(Output.ToString(), Does.Contain("line 3: accept: done, accepted the invite of T1"));
    }

    [Test]
    public void Accept_fails_when_the_invite_has_ended()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        [
            CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1"),
            new PartyInviteEndedEvent(7, PartyInviteEndReason.Timeout),
        ]);

        var exitCode = Run("play Observer", "accept");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: accept: failed, no party invite is open"));
        Assert.That(Connection.Sent.OfType<AcceptInviteCommand>(), Is.Empty);
    }

    [Test]
    public void Wait_event_ignores_the_invite_that_accept_answered()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1")]);

        var exitCode = Run("play Observer", "accept", "wait-event PartyInvitedEvent 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 3: wait-event PartyInvitedEvent 0.1: failed, no PartyInvitedEvent in 0.1 s"));
    }
}
