using Godot;

namespace Top.Client.Features.Hud;

public partial class HealthBar : HudBar
{
    private const float HighTierFraction = 0.5f;

    private const float LowTierFraction = 0.25f;

    private const double TrailHoldSeconds = 0.5;

    private const double TrailFollowSeconds = 0.8;

    private static readonly StringName HighColor = "high";

    private static readonly StringName MiddleColor = "middle";

    private static readonly StringName LowColor = "low";

    private static readonly StringName TrailColor = "trail";

    private static readonly StringName GlowStyle = "glow";

    private float _trail;

    private Tween? _trailFollow;

    private bool IsLow => Fraction <= LowTierFraction;

    protected override void OnFractionChanged()
    {
        _trailFollow?.Kill();
        _trailFollow = null;

        if (Fraction >= _trail || !IsInsideTree())
        {
            _trail = Fraction;

            return;
        }

        _trailFollow = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _trailFollow.TweenInterval(TrailHoldSeconds);
        _trailFollow.TweenMethod(Callable.From<float>(MoveTrail), _trail, Fraction, TrailFollowSeconds);
    }

    protected override void DrawUnderTrack(Vector2[] shape)
    {
        var width = Size.X * Mathf.Max(Fraction, _trail);

        if (IsLow && width > 0f)
        {
            DrawStyleBox(GetThemeStylebox(GlowStyle), new Rect2(0f, 0f, width, Size.Y));
        }
    }

    protected override void DrawFill(Vector2[] shape)
    {
        DrawSpan(shape, _trail, GetThemeColor(TrailColor));
        DrawSpan(shape, Fraction, GetThemeColor(SelectTierColor()));
    }

    private StringName SelectTierColor()
    {
        return Fraction > HighTierFraction ? HighColor : IsLow ? LowColor : MiddleColor;
    }

    private void MoveTrail(float trail)
    {
        _trail = trail;

        QueueRedraw();
    }
}
