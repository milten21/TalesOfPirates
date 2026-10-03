namespace Top.Client.Cli.Core;

public class StepResult
{
    private StepResult(bool isDone, bool isEndOfRun, string text)
    {
        IsDone = isDone;
        IsEndOfRun = isEndOfRun;
        Text = text;
    }

    public bool IsDone { get; }

    public bool IsEndOfRun { get; }

    public string Text { get; }

    public static StepResult Done(string text)
    {
        return new StepResult(true, false, text);
    }

    public static StepResult EndOfRun(string text)
    {
        return new StepResult(true, true, text);
    }

    public static StepResult Failed(string reason)
    {
        return new StepResult(false, false, reason);
    }

    public override string ToString()
    {
        return $"{(IsDone ? "done" : "failed")}, {Text}";
    }
}
