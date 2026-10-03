using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Tests.Features.Waits.Steps;

public class WaitEventStepTests : ScenarioTest
{
    [Test]
    public void Wait_event_ends_when_the_server_sends_the_event()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new PartyInvitedEvent(7, "T1"), afterDrains: 5);

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 5");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Output.ToString(),
            Does.Contain(
                "line 2: wait-event PartyInvitedEvent 5: done, PartyInvitedEvent { InviterId = 7, InviterName = \"T1\" } arrived"));
    }

    [Test]
    public void Wait_event_fails_at_its_time_limit()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 0.1", "quit");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: wait-event PartyInvitedEvent 0.1: failed, no PartyInvitedEvent in 0.1 s"));
    }

    [Test]
    public void Wait_event_takes_an_event_that_arrived_during_an_earlier_step_only_once()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1")]);

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 1", "wait-event PartyInvitedEvent 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: wait-event PartyInvitedEvent 1: done"));
        Assert.That(Output.ToString(), Does.Contain("line 3: wait-event PartyInvitedEvent 0.1: failed"));
    }

    [Test]
    public void Wait_event_ignores_an_event_that_arrived_before_the_event_of_the_previous_wait()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), Appear(50, "Guard"), new PartyInvitedEvent(7, "T1")]);

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 1", "wait-event CharacterAppearedEvent 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 3: wait-event CharacterAppearedEvent 0.1: failed, no CharacterAppearedEvent in 0.1 s"));
    }

    [Test]
    public void Wait_event_takes_an_event_that_arrived_after_the_event_of_the_previous_wait()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1"), Appear(50, "Guard")]);

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 1", "wait-event CharacterAppearedEvent 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
    }

    [Test]
    public void Wait_event_for_an_unknown_event_fails_at_its_line()
    {
        var exitCode = Run("wait-event PartyInvited");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 1: wait-event PartyInvited: failed, no server event is named PartyInvited"));
    }
}
