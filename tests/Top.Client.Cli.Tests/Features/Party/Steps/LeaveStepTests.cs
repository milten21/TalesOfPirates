using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Party.Steps;

public class LeaveStepTests : ScenarioTest
{
    [Test]
    public void Leave_sends_the_party_leave()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
        [
            CreateEnteredMap("Observer", Vector2.Zero),
            new PartyChangedEvent(PartyChangeKind.Joined, "Observer",
                [new PartyMember(12, "T1"), new PartyMember(PlayerId, "Observer")]),
        ]);

        var exitCode = Run("play Observer", "leave");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<LeavePartyCommand>(), Has.Exactly(1).Items);
        Assert.That(Output.ToString(), Does.Contain("line 2: leave: done, left the party of T1"));
    }

    [Test]
    public void Leave_fails_when_the_character_is_in_no_party()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "leave");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 2: leave: failed, Observer is in no party"));
        Assert.That(Connection.Sent.OfType<LeavePartyCommand>(), Is.Empty);
    }
}
