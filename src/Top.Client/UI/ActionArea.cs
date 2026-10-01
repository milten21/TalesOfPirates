using System.Linq;
using Godot;

namespace Top.Client.UI;

public partial class ActionArea : Control
{
    private const double FadeSeconds = 0.15;

    private Tween? _fade;

    public void Switch(Control next)
    {
        _fade?.Kill();
        _fade = CreateTween().SetParallel();

        foreach (var leaving in GetChildren().OfType<Control>().Where(child => child != next && child.Visible))
        {
            _fade.TweenProperty(leaving, "modulate:a", 0f, FadeSeconds);
            _fade.TweenCallback(Callable.From(leaving.Hide)).SetDelay(FadeSeconds);
        }

        if (!next.Visible)
        {
            next.Modulate = new Color(next.Modulate, 0f);

            next.Show();
        }

        _fade.TweenProperty(next, "modulate:a", 1f, FadeSeconds);
    }
}
