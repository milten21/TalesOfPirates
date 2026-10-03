using System;
using System.Numerics;
using Top.Client.Cli.Core;
using Top.Client.Messages;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Features.Play.Steps;

public class MoveStep(Vector2 target, TimeSpan timeLimit) : IParsableStep<MoveStep>
{
    private const MoveStates MoveStopFailures =
        MoveStates.Block | MoveStates.Cancel | MoveStates.NoTarget | MoveStates.CannotMove;

    public static string Usage => "move <x> <z> [timeout]";

    public static string Description => "walk to the point and wait until the move ends. A block fails it";

    public static MoveStep Parse(StepArguments arguments)
    {
        return new MoveStep(new Vector2(arguments.ReadMeters(0), arguments.ReadMeters(1)),
            arguments.ReadTimeLimit(2));
    }

    public StepResult Run(ScenarioSession session)
    {
        var state = session.GetState<PlayState>();
        var worldId = state.RequireCharacter().WorldId;

        session.Events.RemoveAll(serverEvent => IsEndOfMove(serverEvent, worldId));
        session.Send(new MoveCommand(worldId, [state.Position, target]));

        var moved = session.WaitForReply<CharacterMovedEvent, ActionFailedEvent>(timeLimit, "end of the move",
            refused => $"the server refused the move ({refused.Reason})",
            isDone: moved => IsEndOfMove(moved, worldId), isFailed: failed => IsEndOfMove(failed, worldId));

        if ((moved.MoveState & MoveStopFailures) != 0)
        {
            throw new StepFailedException(
                $"the move stopped ({moved.MoveState}) at {ValueText.FromPoint(state.Position)}");
        }

        return StepResult.Done($"arrived at {ValueText.FromPoint(state.Position)}");
    }

    private static bool IsEndOfMove(IServerEvent serverEvent, uint worldId)
    {
        return serverEvent switch
        {
            CharacterMovedEvent moved => moved.WorldId == worldId && moved.MoveState != MoveStates.None,
            ActionFailedEvent failed => failed.WorldId == worldId,
            _ => false,
        };
    }
}
