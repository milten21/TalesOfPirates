using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Top.Client.Cli.Tests;

public class StepExecutorTests
{
    [Test]
    public void Step_help_lists_each_step_with_its_usage_and_description_in_one_column()
    {
        var output = new StringWriter();

        StepExecutor.WriteStepHelp(output);

        var lines = output.ToString().Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        var say = lines.Single(line => line.StartsWith("  say <text...> "));
        var use = lines.Single(line => line.StartsWith("  use <bag slot> [timeout] "));
        Assert.That(say, Does.EndWith("say the rest of the line in local chat, also a GM command"));
        Assert.That(use, Does.EndWith("use or equip the item in the bag slot, from 0"));
        Assert.That(say.IndexOf("say the", 4), Is.EqualTo(use.IndexOf("use or", 4)));
        Assert.That(output.ToString(), Does.Contain("A step with no [timeout] waits 30 s at most."));
    }
}
