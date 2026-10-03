using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Play;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Features.Party.Steps;

public class InviteStep(string characterName) : IParsableStep<InviteStep>
{
    public static string Usage => "invite <name>";

    public static string Description => "invite the character with that name to the party";

    public static InviteStep Parse(StepArguments arguments)
    {
        return new InviteStep(arguments.ReadText(0));
    }

    public StepResult Run(ScenarioSession session)
    {
        session.GetState<PlayState>().RequireCharacter();

        session.Send(new InviteToPartyCommand(characterName));

        return StepResult.Done($"invited {characterName} to the party");
    }
}
