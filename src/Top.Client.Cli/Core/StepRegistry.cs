using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Top.Client.Cli.Core;

public class StepRegistry
{
    private const string RequiredMark = "<";

    private const string OptionalMark = "[";

    private const string RestMark = "...>";

    private const int HelpColumnGap = 2;

    private readonly OrderedDictionary<string, StepDefinition> _definitions = [];

    public void Add<TStep>() where TStep : IParsableStep<TStep>
    {
        var words = SplitWords(TStep.Usage, int.MaxValue);
        var parameters = words.Skip(1).ToArray();
        var requiredCount = parameters.Count(parameter => parameter.StartsWith(RequiredMark, StringComparison.Ordinal));
        var optionalCount = parameters.Count(parameter => parameter.StartsWith(OptionalMark, StringComparison.Ordinal));
        var takesRest = parameters.Length > 0 && parameters[^1].EndsWith(RestMark, StringComparison.Ordinal);

        _definitions.Add(words[0], new StepDefinition(TStep.Usage, TStep.Description, requiredCount,
            requiredCount + optionalCount, takesRest ? requiredCount + optionalCount : int.MaxValue,
            arguments => TStep.Parse(arguments)));
    }

    public void WriteHelp(TextWriter output)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        var usageWidth = _definitions.Values.Max(definition => definition.Usage.Length) + HelpColumnGap;

        foreach (var definition in _definitions.Values)
        {
            output.WriteLine($"  {definition.Usage.PadRight(usageWidth)}{definition.Description}");
        }
    }

    public IStep Parse(string line)
    {
        var words = SplitWords(line, 2);

        if (words.Length == 0)
        {
            throw new StepFailedException("the line has no step");
        }

        if (!_definitions.TryGetValue(words[0], out var definition))
        {
            throw new StepFailedException($"unknown step '{words[0]}'");
        }

        var arguments = words.Length > 1 ? SplitWords(words[1], definition.ArgumentLimit) : [];

        if (arguments.Length < definition.MinimumCount || arguments.Length > definition.MaximumCount)
        {
            throw new StepFailedException($"the step is '{definition.Usage}'");
        }

        return definition.Parse(new StepArguments(arguments));
    }

    private static string[] SplitWords(string text, int limit)
    {
        return text.Split((char[]?)null, limit, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private record StepDefinition(string Usage, string Description, int MinimumCount, int MaximumCount,
        int ArgumentLimit, Func<StepArguments, IStep> Parse);
}
