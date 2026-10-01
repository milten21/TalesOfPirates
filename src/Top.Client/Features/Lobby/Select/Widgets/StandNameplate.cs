using Godot;

namespace Top.Client.Features.Lobby.Select.Widgets;

public partial class StandNameplate : PanelContainer
{
    private const double LookSeconds = 0.3;

    private const float FadedTextAlpha = 0.3f;

    private static readonly StringName PanelStyle = "panel";

    private static readonly StringName SelectedType = "NameplateSelected";

    private static readonly StringName FadedType = "NameplateFaded";

    private StyleBoxFlat _style = new StyleBoxFlat();

    private StyleBoxFlat? _normalStyle;

    private StyleBoxFlat? _selectedStyle;

    private StyleBoxFlat? _fadedStyle;

    private bool _isSelected;

    private bool _isFaded;

    private Tween? _look;

    [Signal]
    public delegate void ClickedEventHandler(bool isDoubleClick);

    [Export] public Control Lines { get; set; } = null!;

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Label MetaLabel { get; set; } = null!;

    [Export] public Control Diamond { get; set; } = null!;

    public override void _Ready()
    {
        _normalStyle = GetThemeStylebox(PanelStyle) as StyleBoxFlat;
        _selectedStyle = GetThemeStylebox(PanelStyle, SelectedType) as StyleBoxFlat;
        _fadedStyle = GetThemeStylebox(PanelStyle, FadedType) as StyleBoxFlat;

        if (_normalStyle != null)
        {
            _style = (StyleBoxFlat)_normalStyle.Duplicate();
        }

        AddThemeStyleboxOverride(PanelStyle, _style);

        Diamond.Modulate = Colors.Transparent;
    }

    public override void _GuiInput(InputEvent received)
    {
        if (received is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            return;
        }

        AcceptEvent();

        EmitSignal(SignalName.Clicked, click.DoubleClick);
    }

    public void ShowCharacter(string name, string meta)
    {
        NameLabel.Text = name;
        MetaLabel.Text = meta;

        Visible = true;

        ResetSize();
    }

    public void ShowSelected(bool isSelected)
    {
        _isSelected = isSelected;

        ShowLook();
    }

    public void ShowFaded(bool isFaded)
    {
        _isFaded = isFaded;

        ShowLook();
    }

    private void ShowLook()
    {
        var target = _isFaded ? _fadedStyle : _isSelected ? _selectedStyle : _normalStyle;

        if (target == null)
        {
            return;
        }

        _look?.Kill();
        _look = CreateTween().SetParallel();

        _look.TweenProperty(_style, "bg_color", target.BgColor, LookSeconds);
        _look.TweenProperty(_style, "border_color", target.BorderColor, LookSeconds);
        _look.TweenProperty(Diamond, "modulate:a", _isSelected && !_isFaded ? 1f : 0f, LookSeconds);
        _look.TweenProperty(Lines, "modulate:a", _isFaded ? FadedTextAlpha : 1f, LookSeconds);
    }
}
