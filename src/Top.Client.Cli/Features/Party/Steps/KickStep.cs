using System.Linq;
using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Play;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Features.Party.Steps;

public class KickStep(string memberName) : IParsableStep<KickStep>
{
    public static string Usage => "kick <name>";

    public static string Description => "kick the party member with that name";

    public static KickStep Parse(StepArguments arguments)
    {
        return new KickStep(arguments.ReadText(0));
    }

    public StepResult Run(ScenarioSession session)
    {
        var player = session.GetState<PlayState>().RequireCharacter();
        var members = session.GetState<PartyState>().RequireMembersOf(player);

        if (members[0].CharacterId != player.WorldId)
        {
            throw new StepFailedException($"only the party leader {members[0].Name} can kick");
        }

        var others = members.Where(member => member.CharacterId != player.WorldId).ToList();
        var kicked = others.Find(member => member.Name == memberName);

        if (kicked == null)
        {
            var names = string.Join(", ", others.Select(member => member.Name));

            throw new StepFailedException($"the party has no other member named {memberName}, only: {names}");
        }

        session.Send(new KickPartyMemberCommand(kicked.CharacterId));

        return StepResult.Done($"kicked {memberName} from the party");
    }
}
