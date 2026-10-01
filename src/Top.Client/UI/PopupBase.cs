using System.Linq;
using Godot;

namespace Top.Client.UI;

public partial class PopupBase : PopupPanel
{
    private const double FadeSeconds = 0.15;

    private Tween? _fade;

    private bool _isFullScreen;

    public override void _Ready()
    {
        SetFlag(Flags.Popup, false);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (IsClosing)
        {
            if (inputEvent is InputEventKey or InputEventJoypadButton)
            {
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (inputEvent.IsActionPressed("ui_cancel"))
        {
            Close();
        }
        else if (inputEvent.IsActionPressed("ui_accept"))
        {
            Accept();
        }
        else if (IsDirection(inputEvent) && GuiGetFocusOwner() == null && NavigationStart is { } control)
        {
            control.GrabFocus();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    protected bool IsClosing { get; private set; }

    protected virtual Control? FirstFocus => null;

    protected virtual Control? NavigationStart => null;

    protected virtual void Accept()
    {
    }

    protected void OpenCentered()
    {
        _fade?.Kill();

        _isFullScreen = false;
        IsClosing = false;

        ResetSize();
        PopupCentered();

        FocusFirst();
    }

    protected void OpenFullScreen()
    {
        _isFullScreen = true;
        IsClosing = false;

        PopupCenteredRatio(1f);

        foreach (var item in GetChildren().OfType<CanvasItem>())
        {
            item.Modulate = Colors.Transparent;
        }

        Fade(1f);
        FocusFirst();
    }

    protected void Close()
    {
        if (IsClosing)
        {
            return;
        }

        if (!_isFullScreen)
        {
            Hide();

            return;
        }

        IsClosing = true;

        Fade(0f).Chain().TweenCallback(Callable.From(FinishClose));
    }

    private static bool IsDirection(InputEvent inputEvent)
    {
        return inputEvent.IsActionPressed("ui_left") || inputEvent.IsActionPressed("ui_right")
            || inputEvent.IsActionPressed("ui_up") || inputEvent.IsActionPressed("ui_down");
    }

    private Tween Fade(float alpha)
    {
        _fade?.Kill();
        _fade = CreateTween().SetParallel();

        foreach (var item in GetChildren().OfType<CanvasItem>())
        {
            _fade.TweenProperty(item, "modulate:a", alpha, FadeSeconds);
        }

        return _fade;
    }

    private void FinishClose()
    {
        IsClosing = false;

        Hide();
    }

    private void FocusFirst()
    {
        if (FirstFocus is { } control)
        {
            Callable.From(control.GrabFocus).CallDeferred();
        }
    }
}
