using NUnit.Framework;

namespace Top.Client.Cli.Tests;

public class ScenarioRunnerTests : ScenarioTest
{
    [Test]
    public void A_failed_step_stops_the_run_at_its_line_with_a_non_zero_exit_code()
    {
        ReplyToLogIn("T1", "Observer");

        var exitCode = Run("# second player", "", "play Nobody", "quit");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 3: play Nobody: failed, the account godot has no character named Nobody, only: T1, Observer"));
        Assert.That(Output.ToString(), Does.Not.Contain("line 4"));
        Assert.That(Connection.IsOpen, Is.False);
    }

    [Test]
    public void An_unknown_step_fails_at_its_line_before_any_step_runs()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "jump 3");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: jump 3: failed, unknown step 'jump'"));
        Assert.That(Connection.Sent, Is.Empty);
    }
}
