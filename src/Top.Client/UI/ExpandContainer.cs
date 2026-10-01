using System.Linq;
using Godot;

namespace Top.Client.UI;

public partial class ExpandContainer : Container
{
    private const double ChangeSeconds = 0.2;

    private float _openness;

    private Tween? _change;

    public bool IsExpanded { get; private set; }

    public override void _Ready()
    {
        ClipContents = true;

        IsExpanded = Visible;

        _openness = IsExpanded ? 1f : 0f;

        Modulate = Modulate with { A = _openness };
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren)
        {
            return;
        }

        foreach (var child in GetChildren().OfType<Control>())
        {
            FitChildInRect(child, new Rect2(0f, 0f, Size.X, child.GetCombinedMinimumSize().Y));
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        var content = GetChildren().OfType<Control>()
            .Where(child => child.Visible)
            .Aggregate(Vector2.Zero, (size, child) => size.Max(child.GetCombinedMinimumSize()));

        return new Vector2(content.X, content.Y * _openness);
    }

    public void Expand()
    {
        Change(isExpanded: true);
    }

    public void Collapse()
    {
        Change(isExpanded: false);
    }

    private void Change(bool isExpanded)
    {
        if (IsExpanded == isExpanded)
        {
            return;
        }

        IsExpanded = isExpanded;

        float target = isExpanded ? 1f : 0f;

        Visible = true;

        _change?.Kill();
        _change = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

        _change.TweenMethod(Callable.From<float>(SetOpenness), _openness, target, ChangeSeconds);
        _change.TweenProperty(this, "modulate:a", target, ChangeSeconds);

        if (!isExpanded)
        {
            _change.Chain().TweenCallback(Callable.From(Hide));
        }
    }

    private void SetOpenness(float openness)
    {
        _openness = openness;

        UpdateMinimumSize();
    }
}
