using System.Linq;
using Godot;
using Top.Client.Features.MainMenu.Servers.Widgets;

namespace Top.Client.Features.MainMenu;

public partial class StepBlock : VBoxContainer
{
    private const double FadeSeconds = 0.15;

    private const double ResizeSeconds = 0.25;

    private Control? _page;

    private Tween? _change;

    [Signal]
    public delegate void AcceptRequestedEventHandler();

    [Signal]
    public delegate void CancelRequestedEventHandler();

    [Export] public ServerLine ServerLine { get; set; } = null!;

    [Export] public Label Subtitle { get; set; } = null!;

    [Export] public Control Pages { get; set; } = null!;

    public override void _Ready()
    {
        foreach (var page in Pages.GetChildren().OfType<Control>())
        {
            page.MinimumSizeChanged += FitPages;
        }

        _page = Pages.GetChildren().OfType<Control>().FirstOrDefault(page => page.Visible);

        FitPages();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed("ui_cancel"))
        {
            return;
        }

        EmitSignal(SignalName.CancelRequested);

        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed("ui_accept"))
        {
            return;
        }

        EmitSignal(SignalName.AcceptRequested);

        GetViewport().SetInputAsHandled();
    }

    public void ShowSubtitle(string text)
    {
        Subtitle.Text = text;
        Subtitle.Visible = true;
    }

    public void ShowPage(Control page)
    {
        if (page == _page)
        {
            return;
        }

        _change?.Kill();

        var leaving = _page;

        _page = page;

        HidePagesExcept(page, leaving);

        if (!page.Visible)
        {
            page.Modulate = Colors.Transparent;
            page.Visible = true;
        }

        Pages.ClipContents = true;

        _change = CreateTween().SetParallel();

        _change.TweenProperty(page, "modulate:a", 1f, FadeSeconds);
        _change.TweenProperty(Pages, "custom_minimum_size:y", MeasurePage(page), ResizeSeconds)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        if (leaving is not null)
        {
            _change.TweenProperty(leaving, "modulate:a", 0f, FadeSeconds);
        }

        _change.Chain().TweenCallback(Callable.From(FinishChange));
    }

    private void FinishChange()
    {
        _change = null;

        HidePagesExcept(_page, null);

        Pages.ClipContents = false;

        FitPages();
    }

    private void FitPages()
    {
        if (_change is not null)
        {
            return;
        }

        Pages.CustomMinimumSize = new Vector2(0f, MeasurePage(_page));
    }

    private void HidePagesExcept(Control? shown, Control? leaving)
    {
        foreach (var page in Pages.GetChildren().OfType<Control>())
        {
            if (page != shown && page != leaving)
            {
                page.Visible = false;
            }
        }
    }

    private static float MeasurePage(Control? page)
    {
        return page?.GetCombinedMinimumSize().Y ?? 0f;
    }
}
