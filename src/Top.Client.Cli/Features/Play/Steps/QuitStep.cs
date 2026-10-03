using Top.Client.Cli.Core;

namespace Top.Client.Cli.Features.Play.Steps;

public class QuitStep : IParsableStep<QuitStep>
{
    public static string Usage => "quit";

    public static string Description => "close the connection and end the run";

    public static QuitStep Parse(StepArguments arguments)
    {
        return new QuitStep();
    }

    public StepResult Run(ScenarioSession session)
    {
        session.Close();

        return StepResult.EndOfRun("the connection is closed");
    }
}
