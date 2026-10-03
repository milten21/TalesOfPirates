using System;
using System.Collections.Generic;
using System.Globalization;

namespace Top.Client.Cli.Core;

public class StepArguments(IReadOnlyList<string> values)
{
    private const double MaximumSeconds = 24 * 60 * 60;

    private const int MaximumBagSlot = short.MaxValue;

    public string ReadText(int index)
    {
        return values[index];
    }

    public float ReadMeters(int index)
    {
        if (float.TryParse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var meters) &&
            float.IsFinite(meters))
        {
            return meters;
        }

        throw new StepFailedException($"'{values[index]}' is not a number of meters");
    }

    public int ReadBagSlot(int index)
    {
        if (int.TryParse(values[index], NumberStyles.None, CultureInfo.InvariantCulture, out var bagSlot) &&
            bagSlot <= MaximumBagSlot)
        {
            return bagSlot;
        }

        throw new StepFailedException($"'{values[index]}' is not a bag slot from 0 to {MaximumBagSlot}");
    }

    public TimeSpan ReadSeconds(int index)
    {
        if (double.TryParse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds is >= 0 and <= MaximumSeconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        throw new StepFailedException($"'{values[index]}' is not a number of seconds");
    }

    public TimeSpan ReadTimeLimit(int index)
    {
        if (index >= values.Count)
        {
            return ScenarioSession.DefaultTimeLimit;
        }

        var timeLimit = ReadSeconds(index);

        return timeLimit > TimeSpan.Zero
            ? timeLimit
            : throw new StepFailedException($"the time limit must be more than 0 seconds, got '{values[index]}'");
    }

    public Type ReadEventType(int index)
    {
        return ServerEventType.Find(values[index]) ??
               throw new StepFailedException($"no server event is named {values[index]}");
    }
}
