using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Tests.Features.Play.Steps;

public class MoveStepTests : ScenarioTest
{
    [Test]
    public void Move_walks_from_the_last_reached_point_to_the_target_in_meters()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<MoveCommand>(command =>
            [new CharacterMovedEvent(PlayerId, [command.Path[^1]], MoveStates.Arrive, null)]);

        var exitCode = Run("play Observer", "move 2130.5 2786", "move 2134 2790");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());

        var moves = Connection.Sent.OfType<MoveCommand>().ToArray();
        Assert.That(moves.Select(move => move.WorldId), Is.EqualTo(new[] { PlayerId, PlayerId }));
        Assert.That(moves[0].Path, Is.EqualTo(new[] { new Vector2(2100f, 2780f), new Vector2(2130.5f, 2786f) }));
        Assert.That(moves[1].Path, Is.EqualTo(new[] { new Vector2(2130.5f, 2786f), new Vector2(2134f, 2790f) }));
        Assert.That(Output.ToString(), Does.Contain("line 3: move 2134 2790: done, arrived at (2134, 2790)"));
    }

    [Test]
    public void A_blocked_move_fails()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<MoveCommand>(_ =>
            [new CharacterMovedEvent(PlayerId, [new Vector2(2110f, 2782f)], MoveStates.Block, null)]);

        var exitCode = Run("play Observer", "move 2130 2786");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: move 2130 2786: failed, the move stopped (Block) at (2110, 2782)"));
    }

    [Test]
    public void Move_fails_at_its_time_limit_when_the_move_does_not_end()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "move 2130 2786 0.1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: move 2130 2786 0.1: failed, no end of the move in 0.1 s"));
    }

    [Test]
    public void Move_before_play_fails()
    {
        var exitCode = Run("move 2130 2786");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 1: move 2130 2786: failed, no character is in the world, use play first"));
    }

    [Test]
    public void Move_ignores_a_failed_action_that_arrived_before_the_move()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        [
            CreateEnteredMap("Observer", new Vector2(2100f, 2780f)),
            new ActionFailedEvent(PlayerId, 1, ActionFailureReason.AlreadyActing),
        ]);
        Connection.ReplyTo<MoveCommand>(command =>
            [new CharacterMovedEvent(PlayerId, [command.Path[^1]], MoveStates.Arrive, null)]);

        var exitCode = Run("play Observer", "move 2130 2786");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Output.ToString(), Does.Contain("line 2: move 2130 2786: done, arrived at (2130, 2786)"));
    }
}
