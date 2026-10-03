using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Tests.Features.Waits.Steps;

public class WaitActorStepTests : ScenarioTest
{
    [Test]
    public void Wait_actor_ends_when_the_actor_appears()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(Appear(51, "T1"), afterDrains: 5);

            return [CreateEnteredMap("Observer", Vector2.Zero), Appear(50, "Guard")];
        });

        var exitCode = Run("play Observer", "wait-actor T1 5");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Output.ToString(), Does.Contain("line 2: wait-actor T1 5: done, T1 is in sight"));
    }

    [Test]
    public void Wait_actor_ends_at_once_for_an_actor_already_in_sight()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ => [CreateEnteredMap("Observer", Vector2.Zero), Appear(51, "T1")]);

        var exitCode = Run("play Observer", "wait-event CharacterAppearedEvent 1", "wait-actor T1 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
    }

    [Test]
    public void Wait_actor_fails_at_its_time_limit_for_an_actor_that_left_sight()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        [
            CreateEnteredMap("Observer", Vector2.Zero), Appear(51, "T1"),
            new CharacterDisappearedEvent(51, SeeType.New),
        ]);

        var exitCode = Run("play Observer", "wait-actor T1 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: wait-actor T1 0.1: failed, no T1 in sight in 0.1 s"));
    }

    [Test]
    public void Wait_actor_fails_after_a_lost_connection_for_an_actor_that_was_in_sight()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new DisconnectedEvent(DisconnectReason.ServerClosed), afterDrains: 2);

            return [CreateEnteredMap("Observer", Vector2.Zero), Appear(51, "T1")];
        });

        var exitCode = Run("play Observer", "wait-actor T1 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain(
            "line 2: wait-actor T1 0.1: failed, the connection was lost (ServerClosed) while waiting for T1 in sight"));
    }
}
