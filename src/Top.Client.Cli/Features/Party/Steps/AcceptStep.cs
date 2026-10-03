using Top.Client.Cli.Core;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Features.Party.Steps;

public class AcceptStep : IParsableStep<AcceptStep>
{
    public static string Usage => "accept";

    public static string Description => "accept the open party invite";

    public static AcceptStep Parse(StepArguments arguments)
    {
        return new AcceptStep();
    }

    public StepResult Run(ScenarioSession session)
    {
        var invite = session.GetState<PartyInviteState>().Take();

        session.Send(new AcceptInviteCommand(invite.InviterId));

        return StepResult.Done($"accepted the invite of {invite.InviterName}");
    }
}
