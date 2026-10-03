using Top.Client.Cli.Core;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Features.Party.Steps;

public class DeclineStep : IParsableStep<DeclineStep>
{
    public static string Usage => "decline";

    public static string Description => "decline the open party invite";

    public static DeclineStep Parse(StepArguments arguments)
    {
        return new DeclineStep();
    }

    public StepResult Run(ScenarioSession session)
    {
        var invite = session.GetState<PartyInviteState>().Take();

        session.Send(new DeclineInviteCommand(invite.InviterId));

        return StepResult.Done($"declined the invite of {invite.InviterName}");
    }
}
