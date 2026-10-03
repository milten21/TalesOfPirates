using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Top.Client.Messages.Chat.Commands;

namespace Top.Client.Cli.Tests.Features.Chat.Steps;

public class SayStepTests : ScenarioTest
{
    [Test]
    public void Say_sends_the_rest_of_the_line_as_one_chat_line()
    {
        ReplyToLogIn("Observer");

        var exitCode = Run("play Observer", "say   Hello  there,   T1  ");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(Connection.Sent.OfType<SayCommand>().Single().Text, Is.EqualTo("Hello  there,   T1"));
        Assert.That(Output.ToString(), Does.Contain("line 2: say   Hello  there,   T1: done, said the line"));
    }

    [Test]
    public void Say_with_no_text_fails_at_its_line()
    {
        var exitCode = Run("say");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(), Does.Contain("line 1: say: failed, the step is 'say <text...>'"));
    }

    [Test]
    public void Say_fails_when_no_character_is_in_the_world()
    {
        var exitCode = Run("say &make 3,1");

        Assert.That(exitCode, Is.EqualTo(ExitCode.StepFailed));
        Assert.That(Output.ToString(),
            Does.Contain("line 1: say &make 3,1: failed, no character is in the world, use play first"));
        Assert.That(Connection.Sent.OfType<SayCommand>(), Is.Empty);
    }

    [Test]
    public void Two_says_are_at_least_the_say_interval_apart()
    {
        var stopwatch = Stopwatch.StartNew();
        var sentAt = new List<TimeSpan>();
        ReplyToLogIn("Observer");
        Connection.ReplyTo<SayCommand>(_ =>
        {
            sentAt.Add(stopwatch.Elapsed);

            return [];
        });

        var exitCode = Run("play Observer", "say &make 3,1", "say &attr 0,10");

        Assert.That(exitCode, Is.EqualTo(ExitCode.Success), Output.ToString());
        Assert.That(sentAt[1] - sentAt[0], Is.GreaterThanOrEqualTo(SayInterval));
        Assert.That(Output.ToString(), Does.Match(@"line 3: say &attr 0,10: done, said the line after [\d.]+ s"));
    }
}
