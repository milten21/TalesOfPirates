using System.Linq;
using NUnit.Framework;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Play.Steps;

public class PlayStepTests : ScenarioTest
{
    [Test]
    public void Play_logs_in_and_enters_with_the_index_of_the_named_character()
    {
        ReplyToLogIn("T1", "Observer", "Third");

        var exitCode = Run("play Observer");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Host, Is.EqualTo("127.0.0.1"));
        Assert.That(Connection.Port, Is.EqualTo(1973));

        var logIn = Connection.Sent.OfType<LogInCommand>().Single();
        Assert.That(logIn.Account, Is.EqualTo("godot"));
        Assert.That(logIn.Password, Is.EqualTo("secret"));
        Assert.That(Connection.Sent.OfType<BeginPlayCommand>().Single().CharacterIndex, Is.EqualTo(1));
    }
}
