using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Top.Client.Features.Hud;

public partial class HudBar : Control
{
    private static readonly StringName TrackColor = "track";

    private static readonly StringName FillColor = "fill";

    private static readonly StringName HighlightColor = "highlight";

    private static readonly StringName SlantConstant = "slant";

    private static readonly StringName HighlightHeightConstant = "highlight_height";

    private static readonly StringName HoverMarginConstant = "hover_margin";

    private float _fraction;

    [Export] public Control? Tip { get; set; }

    [Export] public Label? AmountLabel { get; set; }

    public float Fraction
    {
        get => _fraction;
        set
        {
            var clamped = Mathf.Clamp(value, 0f, 1f);

            if (clamped == _fraction)
            {
                return;
            }

            _fraction = clamped;

            OnFractionChanged();
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        if (Tip == null)
        {
            return;
        }

        Tip.Hide();

        MouseEntered += Tip.Show;
        MouseExited += Tip.Hide;
    }

    public override bool _HasPoint(Vector2 point)
    {
        var margin = GetThemeConstant(HoverMarginConstant);

        return new Rect2(0f, -margin, Size.X, Size.Y + (2 * margin)).HasPoint(point);
    }

    public override void _Draw()
    {
        if (Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        var shape = BuildShape();

        DrawUnderTrack(shape);

        DrawColoredPolygon(shape, GetThemeColor(TrackColor));

        DrawFill(shape);

        var highlightHeight = GetThemeConstant(HighlightHeightConstant);

        if (highlightHeight > 0)
        {
            DrawColoredPolygon(Clip(shape, Vector2.Down, highlightHeight), GetThemeColor(HighlightColor));
        }
    }

    public void UpdateAmount(int current, int maximum)
    {
        Fraction = maximum > 0 ? (float)current / maximum : 0f;

        AmountLabel?.Text = string.Create(CultureInfo.InvariantCulture, $"{current} / {maximum}");
    }

    protected virtual void OnFractionChanged()
    {
    }

    protected virtual void DrawUnderTrack(Vector2[] shape)
    {
    }

    protected virtual void DrawFill(Vector2[] shape)
    {
        DrawSpan(shape, Fraction, GetThemeColor(FillColor));
    }

    protected void DrawSpan(Vector2[] shape, float fraction, Color color)
    {
        var width = Size.X * fraction;

        if (width > 0f)
        {
            DrawColoredPolygon(Clip(shape, Vector2.Right, width), color);
        }
    }

    private static Vector2[] Clip(Vector2[] polygon, Vector2 normal, float limit)
    {
        var clipped = new List<Vector2>(polygon.Length + 1);

        for (var index = 0; index < polygon.Length; index++)
        {
            var current = polygon[index];
            var next = polygon[(index + 1) % polygon.Length];
            var currentDistance = current.Dot(normal) - limit;
            var nextDistance = next.Dot(normal) - limit;

            if (currentDistance <= 0f)
            {
                clipped.Add(current);
            }

            if ((currentDistance < 0f && nextDistance > 0f) || (currentDistance > 0f && nextDistance < 0f))
            {
                clipped.Add(current.Lerp(next, currentDistance / (currentDistance - nextDistance)));
            }
        }

        return clipped.ToArray();
    }

    private Vector2[] BuildShape()
    {
        var size = Size;
        var slant = Mathf.Min(GetThemeConstant(SlantConstant), size.X);

        return [Vector2.Zero, new Vector2(size.X, 0f), new Vector2(size.X - slant, size.Y), new Vector2(0f, size.Y)];
    }
}
