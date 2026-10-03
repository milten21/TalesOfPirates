using System;
using System.Diagnostics;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Waits.Steps;

public class SleepStepTests : ScenarioTest
{
    [Test]
    public void Sleep_waits_its_time_and_keeps_taking_events()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        {
            Connection.Queue(new PartyInvitedEvent(7, "T1"), afterDrains: 3);

            return [CreateEnteredMap("Observer", Vector2.Zero)];
        });

        var stopwatch = Stopwatch.StartNew();
        var exitCode = Run("play Observer", "sleep 0.2", "accept");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(stopwatch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(0.2)));
        Assert.That(Output.ToString(), Does.Contain("line 2: sleep 0.2: done, slept 0.2 s"));
    }
}
