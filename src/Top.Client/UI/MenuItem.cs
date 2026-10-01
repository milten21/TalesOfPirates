using Godot;

namespace Top.Client.UI;

public partial class MenuItem : CaptionButton
{
    private const double DiamondFadeSeconds = 0.12;

    private Tween? _diamondFade;

    [Export] public Control Layout { get; set; } = null!;

    [Export] public Control Diamond { get; set; } = null!;

    public override void _Ready()
    {
        Layout.MinimumSizeChanged += FitLayout;

        MouseEntered += Select;
        MouseExited += Deselect;
        FocusEntered += ShowSelection;
        FocusExited += ShowSelection;

        Diamond.Modulate = Colors.Transparent;

        FitLayout();
    }

    private void Select()
    {
        GrabFocus();
    }

    private void Deselect()
    {
        if (HasFocus())
        {
            ReleaseFocus();
        }
    }

    private void ShowSelection()
    {
        _diamondFade?.Kill();
        _diamondFade = CreateTween();
        _diamondFade.TweenProperty(Diamond, "modulate:a", HasFocus() ? 1f : 0f, DiamondFadeSeconds);
    }

    private void FitLayout()
    {
        CustomMinimumSize = new Vector2(Layout.GetCombinedMinimumSize().X, CustomMinimumSize.Y);
    }
}
