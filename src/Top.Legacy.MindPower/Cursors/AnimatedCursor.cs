using System.Collections.Generic;

namespace Top.Legacy.MindPower.Cursors
{
    /// <summary>
    /// The frames of a cursor and the animation steps that show them.
    /// </summary>
    public class AnimatedCursor(IReadOnlyList<CursorFrame> frames, IReadOnlyList<int> steps,
        IReadOnlyList<int> stepDurations)
    {
        public IReadOnlyList<CursorFrame> Frames { get; } = frames;

        public IReadOnlyList<int> Steps { get; } = steps;

        public IReadOnlyList<int> StepDurations { get; } = stepDurations;
    }
}
