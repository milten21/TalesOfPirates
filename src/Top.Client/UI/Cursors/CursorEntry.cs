using System.Collections.Generic;
using Godot;

namespace Top.Client.UI.Cursors;

public class CursorEntry(
    string image,
    Vector2I frame,
    IReadOnlyList<Vector2I> hotspots,
    IReadOnlyList<int>? steps,
    IReadOnlyList<int>? durations,
    int scaleFactor = 1)
{
    public string Image { get; } = image;

    public Vector2I Frame { get; } = frame;

    public IReadOnlyList<Vector2I> Hotspots { get; } = hotspots;

    public CursorPose PoseAt(int frameCount, long elapsedMilliseconds)
    {
        var stepCount = steps?.Count ?? frameCount;

        if (durations == null || stepCount == 0)
        {
            return CreatePose(0, 0);
        }

        var total = 0L;

        for (var step = 0; step < stepCount; step++)
        {
            total += DurationOf(step);
        }

        if (total <= 0)
        {
            return CreatePose(0, 0);
        }

        var position = elapsedMilliseconds % total;

        for (var step = 0; step < stepCount; step++)
        {
            var duration = DurationOf(step);

            if (position < duration)
            {
                return CreatePose(step, (int)(duration - position));
            }

            position -= duration;
        }

        return CreatePose(0, 0);
    }

    private int DurationOf(int step)
    {
        return durations!.Count == 1 ? durations[0] : durations[step];
    }

    private CursorPose CreatePose(int step, int millisecondsToNextStep)
    {
        var frameIndex = steps?[step] ?? step;

        var hotspot = Hotspots.Count == 1 ? Hotspots[0] : Hotspots[frameIndex];

        return new CursorPose(frameIndex, hotspot * scaleFactor, millisecondsToNextStep);
    }
}
