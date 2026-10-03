using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Play.Commands;

namespace Top.Client.Cli.Tests.Features.Inventory.Steps;

public class UseStepTests : ScenarioTest
{
    [Test]
    public void Use_sends_the_item_use_for_the_bag_slot_and_ends_at_the_bag_change()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<UseBagItemCommand>(command => [EmptyBagSlot(PlayerId, command.BagSlot)]);

        var exitCode = Run("play Observer", "use 4");

        var sent = Connection.Sent.OfType<UseBagItemCommand>().Single();
        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That((sent.WorldId, sent.BagSlot), Is.EqualTo((PlayerId, 4)));
        Assert.That(Output.ToString(), Does.Contain("line 2: use 4: done, used the item in bag slot 4"));
    }

    [Test]
    public void Use_fails_with_the_reason_the_server_refused_it()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<UseBagItemCommand>(_ => [new ItemActionFailedEvent(ItemActionFailure.LevelTooLow)]);

        var exitCode = Run("play Observer", "use 4");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: use 4: failed, the server refused the item use (LevelTooLow)"));
    }

    [Test]
    public void Use_does_not_end_at_a_bag_change_from_before_it()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<BeginPlayCommand>(_ =>
            [CreateEnteredMap("Observer", Vector2.Zero), EmptyBagSlot(PlayerId, 0)]);
        Connection.ReplyTo<UseBagItemCommand>(_ => [new ItemActionFailedEvent(ItemActionFailure.WrongBody)]);

        var exitCode = Run("play Observer", "use 0");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: use 0: failed, the server refused the item use (WrongBody)"));
    }

    [Test]
    public void Use_does_not_end_at_the_bag_change_of_another_character()
    {
        ReplyToLogIn("Observer");
        Connection.ReplyTo<UseBagItemCommand>(_ => [EmptyBagSlot(12, 4)]);

        var exitCode = Run("play Observer", "use 4 0.2");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 2: use 4 0.2: failed, no bag change or item failure in 0.2 s"));
    }

    [Test]
    public void Use_fails_when_no_character_is_in_the_world()
    {
        var exitCode = Run("use 4");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 1: use 4: failed, no character is in the world, use play first"));
        Assert.That(Connection.Sent.OfType<UseBagItemCommand>(), Is.Empty);
    }

    [TestCase("-1")]
    [TestCase("1.5")]
    [TestCase("32768")]
    [TestCase("ring")]
    public void Use_of_a_value_that_is_not_a_bag_slot_fails_at_its_line(string bagSlot)
    {
        var exitCode = Run($"use {bagSlot}");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain($"line 1: use {bagSlot}: failed, '{bagSlot}' is not a bag slot from 0 to 32767"));
    }

    private static BagChangedEvent EmptyBagSlot(uint worldId, int bagSlot)
    {
        return new BagChangedEvent(worldId, new BagSync(null, [new BagSlot(bagSlot, null)]));
    }
}
