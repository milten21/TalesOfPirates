using System;
using System.Linq;
using Godot;

namespace Top.Client.Tools.Debug.Performance;

public partial class FrameTimeGraph : Control
{
    private const int Capacity = 300;

    private const float SixtyFramesPerSecondBudget = 1000f / 60f;

    private const float ThirtyFramesPerSecondBudget = 1000f / 30f;

    private const float SampleMilliseconds = SixtyFramesPerSecondBudget;

    private const float Headroom = 1.25f;

    private const float GuideAlpha = 0.3f;

    private static readonly StringName FontColor = "font_color";

    private static readonly StringName LabelType = "Label";

    private readonly float[] _samples = new float[Capacity];

    private readonly Vector2[] _points = new Vector2[Capacity];

    private int _nextSlot;

    private int _filledCount;

    private float _pendingMilliseconds;

    private float _pendingLongestFrame;

    public FrameTimeGraph()
    {
        CustomMinimumSize = new Vector2(0f, 96f);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void AddFrame(float milliseconds)
    {
        _pendingMilliseconds += milliseconds;
        _pendingLongestFrame = Math.Max(_pendingLongestFrame, milliseconds);

        if (_pendingMilliseconds < SampleMilliseconds)
        {
            return;
        }

        _samples[_nextSlot] = _pendingLongestFrame;
        _nextSlot = (_nextSlot + 1) % Capacity;
        _filledCount = Math.Min(_filledCount + 1, Capacity);

        _pendingMilliseconds = 0f;
        _pendingLongestFrame = 0f;

        QueueRedraw();
    }

    public override void _Draw()
    {
        var lineColor = GetThemeColor(FontColor, LabelType);
        var guideColor = lineColor with { A = lineColor.A * GuideAlpha };
        var top = Math.Max(SixtyFramesPerSecondBudget, _samples.Max()) * Headroom;

        DrawGuide(SixtyFramesPerSecondBudget, top, guideColor);
        DrawGuide(ThirtyFramesPerSecondBudget, top, guideColor);

        if (_filledCount < 2)
        {
            return;
        }

        var step = Size.X / (Capacity - 1);
        var oldestSlot = (_nextSlot - _filledCount + Capacity) % Capacity;
        var emptySlots = Capacity - _filledCount;

        for (var index = 0; index < _filledCount; index++)
        {
            var sample = _samples[(oldestSlot + index) % Capacity];

            _points[index] = new Vector2((emptySlots + index) * step, ToHeight(sample, top));
        }

        DrawPolyline(_filledCount == Capacity ? _points : _points[.._filledCount], lineColor);
    }

    private void DrawGuide(float milliseconds, float top, Color color)
    {
        if (milliseconds > top)
        {
            return;
        }

        var height = ToHeight(milliseconds, top);

        DrawLine(new Vector2(0f, height), new Vector2(Size.X, height), color);
    }

    private float ToHeight(float milliseconds, float top)
    {
        return Size.Y - (milliseconds / top * Size.Y);
    }
}
