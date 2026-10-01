using Godot;

namespace Top.Client.UI;

public partial class PulsingDiamond : Control
{
    private const double PulseSeconds = 1.6;

    private const float LowOpacity = 0.35f;

    private Tween? _pulse;

    private bool _isPulsing = true;

    public bool IsPulsing
    {
        get => _isPulsing;
        set
        {
            _isPulsing = value;

            UpdatePulse();
        }
    }

    public override void _Ready()
    {
        VisibilityChanged += UpdatePulse;

        UpdatePulse();
    }

    private void UpdatePulse()
    {
        var shouldPulse = _isPulsing && IsInsideTree() && IsVisibleInTree();

        if (shouldPulse == (_pulse != null))
        {
            return;
        }

        if (shouldPulse)
        {
            _pulse = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            _pulse.TweenProperty(this, "modulate:a", LowOpacity, PulseSeconds / 2);
            _pulse.TweenProperty(this, "modulate:a", 1f, PulseSeconds / 2);
        }
        else
        {
            _pulse!.Kill();
            _pulse = null;

            Modulate = Colors.White;
        }
    }
}
