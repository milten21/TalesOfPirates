using Godot;

namespace Top.Client.UI;

public partial class Toast : PanelContainer
{
    private const double FadeSeconds = 0.15;

    private const double ShowSeconds = 2.8;

    private Tween? _showing;

    [Export] public Label TextLabel { get; set; } = null!;

    public void ShowMessage(string text)
    {
        _showing?.Kill();

        TextLabel.Text = text;

        Modulate = new Color(Modulate, 0f);
        Visible = true;

        OffsetLeft = 0f;
        OffsetRight = 0f;

        _showing = CreateTween();
        _showing.TweenProperty(this, "modulate:a", 1f, FadeSeconds);
        _showing.TweenInterval(ShowSeconds - (FadeSeconds * 2));
        _showing.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
        _showing.TweenCallback(Callable.From(Hide));
    }
}
