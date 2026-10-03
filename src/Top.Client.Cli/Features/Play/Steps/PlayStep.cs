using System.Linq;
using Top.Client.Cli.Core;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Cli.Features.Play.Steps;

public class PlayStep(string characterName) : IParsableStep<PlayStep>
{
    public static string Usage => "play <character>";

    public static string Description => "log in, select the character by name and enter the world";

    public static PlayStep Parse(StepArguments arguments)
    {
        return new PlayStep(arguments.ReadText(0));
    }

    public StepResult Run(ScenarioSession session)
    {
        var state = session.GetState<PlayState>();
        var account = session.Account;

        if (session.IsOpen)
        {
            throw new StepFailedException($"the account {account.Name} is already logged in");
        }

        state.Leave();

        try
        {
            session.Open();
            session.Send(new LogInCommand(account.Name, account.Password));

            var login = session.WaitForReply<LoginSucceededEvent, LoginFailedEvent>(
                ScenarioSession.DefaultTimeLimit, "login reply",
                refused => $"the server refused the log in with code {refused.Code}: {refused.Text}");

            var index = login.Characters.ToList().FindIndex(character => character.Name == characterName);

            if (index < 0)
            {
                var names = string.Join(", ", login.Characters.Select(character => character.Name));

                throw new StepFailedException(
                    $"the account {account.Name} has no character named {characterName}, only: {names}");
            }

            session.Send(new BeginPlayCommand(index));

            var entered = session.WaitForReply<MapEnteredEvent, BeginPlayFailedEvent>(
                ScenarioSession.DefaultTimeLimit, "map entry",
                failed => $"the server refused to let {characterName} enter with code {failed.Code}: {failed.Text}");

            state.Enter(entered.Character);

            return StepResult.Done(
                $"{characterName} entered {entered.MapName} at {ValueText.FromPoint(state.Position)}");
        }
        catch (StepFailedException)
        {
            session.Close();

            throw;
        }
    }
}
