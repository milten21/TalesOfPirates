using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Events;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Tests.Core;

public class ScenarioSessionTests : ScenarioTest
{
    [Test]
    public void A_lost_connection_fails_the_wait()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new DisconnectedEvent(DisconnectReason.ServerClosed), afterDrains: 2);

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 5");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain(
            "line 2: wait-event PartyInvitedEvent 5: failed, the connection was lost (ServerClosed) while waiting for PartyInvitedEvent"));
    }

    [Test]
    public void Wait_event_cannot_take_an_event_after_ten_thousand_newer_events()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new PartyInvitedEvent(7, "T1"), afterDrains: 2);

            for (var i = 0; i < 10_000; i++)
            {
                Connection.Queue(new CharacterMovedEvent(50, [Vector2.One], MoveStates.None, null), afterDrains: 2);
            }

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        var exitCode = Run("play Observer", "wait-event PartyInvitedEvent 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: wait-event PartyInvitedEvent 0.1: failed, no PartyInvitedEvent in 0.1 s"));
    }

    [Test]
    public void A_lost_connection_fails_a_sleep()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new DisconnectedEvent(DisconnectReason.ServerClosed), afterDrains: 3);

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        var exitCode = Run("play Observer", "sleep 0.5");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: sleep 0.5: failed, the connection was lost (ServerClosed) during the sleep"));
    }
}
