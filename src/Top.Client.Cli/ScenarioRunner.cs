using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Top.Client.Cli.Core;

namespace Top.Client.Cli;

public static class ScenarioRunner
{
    private const string CommentMark = "#";

    public static int Run(string scenario, StepExecutor executor, TextWriter output)
    {
        if (!TryParse(scenario, executor, out var lines, out var error))
        {
            output.WriteLine(error);

            return ExitCode.StepFailed;
        }

        foreach (var line in lines)
        {
            var result = executor.Run(line.Step);

            output.WriteLine($"line {line.Number}: {line.Text}: {result}");

            if (!result.IsDone)
            {
                return ExitCode.StepFailed;
            }

            if (result.IsEndOfRun)
            {
                break;
            }
        }

        return ExitCode.Success;
    }

    private static bool TryParse(string scenario, StepExecutor executor, out List<ScenarioLine> lines,
        [NotNullWhen(false)] out string? error)
    {
        var texts = scenario.Split('\n');

        lines = [];
        error = null;

        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i].Trim();

            if (text.Length == 0 || text.StartsWith(CommentMark, StringComparison.Ordinal))
            {
                continue;
            }

            if (!executor.TryParse(text, out var step, out var stepError))
            {
                error = $"line {i + 1}: {text}: {StepResult.Failed(stepError)}";

                return false;
            }

            lines.Add(new ScenarioLine(i + 1, text, step));
        }

        return true;
    }

    private record ScenarioLine(int Number, string Text, IStep Step);
}
