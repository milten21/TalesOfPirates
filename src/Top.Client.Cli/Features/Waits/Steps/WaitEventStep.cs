using System;
using Top.Client.Cli.Core;
using Top.Client.Messages;

namespace Top.Client.Cli.Features.Waits.Steps;

public class WaitEventStep(Type eventType, TimeSpan timeLimit) : IParsableStep<WaitEventStep>
{
    public static string Usage => "wait-event <EventName> [timeout]";

    public static string Description => "wait for a server event by its type name, such as PartyInvitedEvent";

    public static WaitEventStep Parse(StepArguments arguments)
    {
        return new WaitEventStep(arguments.ReadEventType(0), arguments.ReadTimeLimit(1));
    }

    public StepResult Run(ScenarioSession session)
    {
        IServerEvent? arrived = null;

        session.WaitUntil(timeLimit, eventType.Name, () =>
            (arrived = session.Events.TakeWithOlder(serverEvent => serverEvent.GetType() == eventType)) != null);

        return StepResult.Done($"{ValueText.FromEvent(arrived!)} arrived");
    }
}
