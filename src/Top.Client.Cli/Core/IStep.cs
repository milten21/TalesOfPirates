namespace Top.Client.Cli.Core;

public interface IStep
{
    StepResult Run(ScenarioSession session);
}
