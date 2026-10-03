using System;
using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Play;
using Top.Client.Messages;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Events;

namespace Top.Client.Cli.Features.Inventory.Steps;

public class UseStep(int bagSlot, TimeSpan timeLimit) : IParsableStep<UseStep>
{
    public static string Usage => "use <bag slot> [timeout]";

    public static string Description => "use or equip the item in the bag slot, from 0";

    public static UseStep Parse(StepArguments arguments)
    {
        return new UseStep(arguments.ReadBagSlot(0), arguments.ReadTimeLimit(1));
    }

    public StepResult Run(ScenarioSession session)
    {
        var worldId = session.GetState<PlayState>().RequireCharacter().WorldId;

        session.Events.RemoveAll(serverEvent => IsReplyToUse(serverEvent, worldId));
        session.Send(new UseBagItemCommand(worldId, bagSlot));

        session.WaitForReply<BagChangedEvent, ItemActionFailedEvent>(timeLimit, "bag change or item failure",
            refused => $"the server refused the item use ({refused.Failure})",
            isDone: changed => IsReplyToUse(changed, worldId));

        return StepResult.Done($"used the item in bag slot {bagSlot}");
    }

    private static bool IsReplyToUse(IServerEvent serverEvent, uint worldId)
    {
        return serverEvent switch
        {
            BagChangedEvent changed => changed.WorldId == worldId,
            ItemActionFailedEvent => true,
            _ => false,
        };
    }
}
