using Godot;

namespace Top.Client.Tools.Debug;

public partial class DebugWindow : Window
{
    public const float ScrollBarWidth = 10f;

    private const int ContentMargin = 12;

    private static readonly StringName MarginLeft = "margin_left";

    private static readonly StringName MarginTop = "margin_top";

    private static readonly StringName MarginRight = "margin_right";

    private static readonly StringName MarginBottom = "margin_bottom";

    [Signal] public delegate void MenuKeyPressedEventHandler();

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed(DebugOverlay.MenuAction))
        {
            return;
        }

        SetInputAsHandled();

        EmitSignal(SignalName.MenuKeyPressed);
    }

    protected void AddContent(Control content)
    {
        var margin = new MarginContainer();

        margin.AddThemeConstantOverride(MarginLeft, ContentMargin);
        margin.AddThemeConstantOverride(MarginTop, ContentMargin);
        margin.AddThemeConstantOverride(MarginRight, ContentMargin);
        margin.AddThemeConstantOverride(MarginBottom, ContentMargin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddChild(content);

        AddChild(margin);
    }
}
