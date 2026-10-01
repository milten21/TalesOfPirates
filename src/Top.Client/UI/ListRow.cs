using System.Linq;
using Godot;

namespace Top.Client.UI;

public partial class ListRow : Button
{
    [Signal]
    public delegate void ActivatedEventHandler();

    [Export] public Control Layout { get; set; } = null!;

    [Export] public Control? SelectedMark { get; set; }

    public override void _Ready()
    {
        Layout.MinimumSizeChanged += FitLayout;
        Toggled += ShowSelection;
        FocusEntered += Select;

        foreach (var control in Layout.FindChildren("*", owned: false).OfType<Control>()
                     .Where(control => control.FocusMode != FocusModeEnum.None))
        {
            control.FocusEntered += Select;
        }

        FitLayout();
        ShowSelection(ButtonPressed);
    }

    public override void _Notification(int what)
    {
        switch (what)
        {
            case (int)NotificationThemeChanged when Layout is not null:
                FitLayout();
                break;
            case (int)NotificationMouseEnter:
                ShowHover(true);
                break;
            case (int)NotificationMouseExit:
                ShowHover(false);
                break;
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, DoubleClick: true, Pressed: true })
        {
            EmitSignal(SignalName.Activated);

            return;
        }

        if (!inputEvent.IsActionPressed("ui_accept"))
        {
            return;
        }

        ButtonPressed = true;

        AcceptEvent();

        EmitSignal(SignalName.Activated);
    }

    private void Select()
    {
        ButtonPressed = true;
    }

    private void ShowHover(bool isHovered)
    {
        if (isHovered)
        {
            AddThemeStyleboxOverride("normal", GetThemeStylebox("hover"));
            AddThemeStyleboxOverride("pressed", GetThemeStylebox("hover_pressed"));
        }
        else
        {
            RemoveThemeStyleboxOverride("normal");
            RemoveThemeStyleboxOverride("pressed");
        }
    }

    private void ShowSelection(bool isSelected)
    {
        if (SelectedMark is not null)
        {
            SelectedMark.Visible = isSelected;
        }
    }

    private void FitLayout()
    {
        var padding = GetThemeStylebox("normal");

        Layout.OffsetLeft = padding.GetMargin(Side.Left);
        Layout.OffsetTop = padding.GetMargin(Side.Top);
        Layout.OffsetRight = -padding.GetMargin(Side.Right);
        Layout.OffsetBottom = -padding.GetMargin(Side.Bottom);

        CustomMinimumSize = new Vector2(CustomMinimumSize.X,
            Layout.GetCombinedMinimumSize().Y + padding.GetMinimumSize().Y);
    }
}
