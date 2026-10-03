using System;
using System.IO;
using Top.Client.LegacyBridge;
using Top.Logging;

namespace Top.Client.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args is ["--help"] or ["-h"])
        {
            Console.Write(Arguments.Usage);
            Console.WriteLine();
            StepExecutor.WriteStepHelp(Console.Out);

            return ExitCode.Success;
        }

        if (!Arguments.TryParse(args, out var arguments, out var error))
        {
            return ReportBadArguments(error);
        }

        if (!arguments.IsLive && !File.Exists(arguments.ScenarioPath))
        {
            return ReportBadArguments($"no scenario file at '{Path.GetFullPath(arguments.ScenarioPath)}'");
        }

        Log.Writer = new ConsoleLog();

        using var executor = new StepExecutor(new ServerConnection(), arguments.Account, arguments.SayInterval);

        return arguments.IsLive
            ? new LiveListener(executor, arguments.LivePort, Console.Out).RunAsync().GetAwaiter().GetResult()
            : ScenarioRunner.Run(File.ReadAllText(arguments.ScenarioPath), executor, Console.Out);
    }

    private static int ReportBadArguments(string error)
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
        Console.Error.Write(Arguments.Usage);

        return ExitCode.BadArguments;
    }
}
