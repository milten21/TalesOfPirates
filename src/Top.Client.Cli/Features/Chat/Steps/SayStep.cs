using System;
using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Play;
using Top.Client.Messages.Chat.Commands;

namespace Top.Client.Cli.Features.Chat.Steps;

public class SayStep(string text) : IParsableStep<SayStep>
{
    public static string Usage => "say <text...>";

    public static string Description => "say the rest of the line in local chat, also a GM command";

    public static SayStep Parse(StepArguments arguments)
    {
        return new SayStep(arguments.ReadText(0));
    }

    public StepResult Run(ScenarioSession session)
    {
        session.GetState<PlayState>().RequireCharacter();

        var chat = session.GetState<ChatState>();
        var wait = chat.TimeUntilNextSay;

        session.DrainFor(wait, "the wait for the say interval");
        session.Send(new SayCommand(text));
        chat.MarkSay();

        return StepResult.Done(wait > TimeSpan.Zero
            ? $"said the line after {ValueText.FromSeconds(wait)} for the say interval"
            : "said the line");
    }
}
