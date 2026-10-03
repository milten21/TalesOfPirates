namespace Top.Client.Cli.Core;

public interface IParsableStep<out TSelf> : IStep where TSelf : IParsableStep<TSelf>
{
    static abstract string Usage { get; }

    static abstract string Description { get; }

    static abstract TSelf Parse(StepArguments arguments);
}
