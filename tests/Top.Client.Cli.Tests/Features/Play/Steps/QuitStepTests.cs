using System.Linq;
using NUnit.Framework;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Play.Steps;

public class QuitStepTests : ScenarioTest
{
    [Test]
    public void Quit_ends_the_run_and_closes_the_connection()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "quit", "move 1 1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.IsOpen, Is.False);
        Assert.That(Connection.Sent.OfType<MoveCommand>(), Is.Empty);
    }
}
