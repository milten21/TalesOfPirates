using System;
using Top.Client.Cli.Core;

namespace Top.Client.Cli.Features.Waits.Steps;

public class SleepStep(TimeSpan duration) : IParsableStep<SleepStep>
{
    public static string Usage => "sleep <seconds>";

    public static string Description => "wait a fixed time";

    public static SleepStep Parse(StepArguments arguments)
    {
        return new SleepStep(arguments.ReadSeconds(0));
    }

    public StepResult Run(ScenarioSession session)
    {
        session.DrainFor(duration, "the sleep");

        return StepResult.Done($"slept {ValueText.FromSeconds(duration)}");
    }
}
