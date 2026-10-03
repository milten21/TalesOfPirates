using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Party.Steps;

public class DeclineStepTests : ScenarioTest
{
    [Test]
    public void Decline_answers_the_open_party_invite()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), new PartyInvitedEvent(7, "T1")]);

        var exitCode = Run("play Observer", "decline");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<DeclineInviteCommand>().Single().InviterId, Is.EqualTo(7));
    }
}
