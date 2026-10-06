using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace Top.Client.UI.Cursors;

public class CursorEntries
{
    private const int BaseWindowHeight = 1080;

    private readonly IReadOnlyDictionary<string, CursorEntry> _entries;

    private CursorEntries(IReadOnlyDictionary<string, CursorEntry> entries, int scaleFactor)
    {
        _entries = entries;
        ScaleFactor = scaleFactor;
    }

    public int ScaleFactor { get; }

    public static int ScaleFactorFor(int windowHeight)
    {
        return Math.Max(1, windowHeight / BaseWindowHeight);
    }

    public static CursorEntries Read(string json, int scaleFactor = 1)
    {
        using var document = JsonDocument.Parse(json);

        var entries = new Dictionary<string, CursorEntry>();

        foreach (var property in document.RootElement.GetProperty("cursors").EnumerateObject())
        {
            var entry = property.Value;

            var steps = ReadIntegers(entry, "steps");
            var durations = ReadDurations(entry);

            if (steps != null && durations is { Count: > 1 } && durations.Count != steps.Count)
            {
                throw new InvalidOperationException(
                    $"cursor '{property.Name}' has a duration for each step but the counts differ");
            }

            entries[property.Name] = new CursorEntry(entry.GetProperty("image").GetString()!,
                ReadVector(entry.GetProperty("frame")), ReadHotspots(entry.GetProperty("hotspot")), steps,
                durations, scaleFactor);
        }

        return new CursorEntries(entries, scaleFactor);
    }

    public CursorEntry? Resolve(CursorKey key)
    {
        for (var current = key; current != null; current = current.Fallback)
        {
            if (_entries.TryGetValue(current.Name, out var entry))
            {
                return entry;
            }
        }

        return _entries.GetValueOrDefault(CursorKey.Normal.Name);
    }

    public CursorPose? PoseAt(CursorKey key, int frameCount, long elapsedMilliseconds)
    {
        return Resolve(key)?.PoseAt(frameCount, elapsedMilliseconds);
    }

    private static IReadOnlyList<Vector2I> ReadHotspots(JsonElement element)
    {
        return element[0].ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().Select(ReadVector).ToArray()
            : [ReadVector(element)];
    }

    private static IReadOnlyList<int>? ReadIntegers(JsonElement entry, string name)
    {
        return entry.TryGetProperty(name, out var element)
            ? element.EnumerateArray().Select(value => value.GetInt32()).ToArray()
            : null;
    }

    private static IReadOnlyList<int>? ReadDurations(JsonElement entry)
    {
        if (!entry.TryGetProperty("ms", out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.Array ? ReadIntegers(entry, "ms") : [element.GetInt32()];
    }

    private static Vector2I ReadVector(JsonElement element)
    {
        return new Vector2I(element[0].GetInt32(), element[1].GetInt32());
    }
}
