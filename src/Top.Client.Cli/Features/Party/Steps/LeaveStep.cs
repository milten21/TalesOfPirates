using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Play;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Features.Party.Steps;

public class LeaveStep : IParsableStep<LeaveStep>
{
    public static string Usage => "leave";

    public static string Description => "leave the party";

    public static LeaveStep Parse(StepArguments arguments)
    {
        return new LeaveStep();
    }

    public StepResult Run(ScenarioSession session)
    {
        var player = session.GetState<PlayState>().RequireCharacter();
        var members = session.GetState<PartyState>().RequireMembersOf(player);

        session.Send(new LeavePartyCommand());

        return StepResult.Done($"left the party of {members[0].Name}");
    }
}
