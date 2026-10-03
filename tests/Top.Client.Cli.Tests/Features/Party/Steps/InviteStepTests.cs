using System.Linq;
using NUnit.Framework;
using Top.Client.Messages.Party.Commands;

namespace Top.Client.Cli.Tests.Features.Party.Steps;

public class InviteStepTests : ScenarioTest
{
    [Test]
    public void Invite_sends_the_party_invite_with_the_character_name()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "invite T1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<InviteToPartyCommand>().Single().CharacterName, Is.EqualTo("T1"));
        Assert.That(Output.ToString(), Does.Contain("line 2: invite T1: done, invited T1 to the party"));
    }

    [Test]
    public void Invite_fails_when_no_character_is_in_the_world()
    {
        var exitCode = Run("invite T1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 1: invite T1: failed, no character is in the world, use play first"));
        Assert.That(Connection.Sent.OfType<InviteToPartyCommand>(), Is.Empty);
    }
}
