using System;
using Top.Client.Cli.Core;

namespace Top.Client.Cli.Features.Waits.Steps;

public class WaitActorStep(string actorName, TimeSpan timeLimit) : IParsableStep<WaitActorStep>
{
    public static string Usage => "wait-actor <name> [timeout]";

    public static string Description => "wait until the actor is in sight";

    public static WaitActorStep Parse(StepArguments arguments)
    {
        return new WaitActorStep(arguments.ReadText(0), arguments.ReadTimeLimit(1));
    }

    public StepResult Run(ScenarioSession session)
    {
        var sight = session.GetState<SightState>();

        session.WaitUntil(timeLimit, $"{actorName} in sight", () => sight.IsInSight(actorName));

        return StepResult.Done($"{actorName} is in sight");
    }
}
