using Godot;

namespace Top.Client.UI;

public partial class InlineConfirm : MarginContainer
{
    [Signal]
    public delegate void ConfirmedEventHandler();

    [Signal]
    public delegate void KeptEventHandler();

    [Export] public Label QuestionLabel { get; set; } = null!;

    [Export] public Button ConfirmButton { get; set; } = null!;

    [Export] public Button KeepButton { get; set; } = null!;

    private bool _isKeyboardInput;

    public bool IsOpen { get; private set; }

    public bool WasOpenedWithKeyboard { get; private set; }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton)
        {
            _isKeyboardInput = false;
        }
        else if (inputEvent is InputEventKey or InputEventJoypadButton && inputEvent.IsPressed())
        {
            _isKeyboardInput = true;
        }

        if (IsAnswerable() && inputEvent.IsActionPressed("ui_accept") && GetViewport().GuiGetFocusOwner() == null)
        {
            GetViewport().SetInputAsHandled();

            Confirm();
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (!IsAnswerable() || !NavigationInput.IsNavigation(inputEvent) || GetViewport().GuiGetFocusOwner() != null)
        {
            return;
        }

        ConfirmButton.GrabFocus();

        GetViewport().SetInputAsHandled();
    }

    public override void _Ready()
    {
        ConfirmButton.Pressed += Confirm;
        KeepButton.Pressed += Keep;

        LinkButtons();
    }

    public void Ask(string question)
    {
        QuestionLabel.Text = question;

        WasOpenedWithKeyboard = _isKeyboardInput;

        IsOpen = true;

        if (WasOpenedWithKeyboard)
        {
            ConfirmButton.GrabFocus();
        }
        else
        {
            GetViewport().GuiReleaseFocus();
        }
    }

    public void Keep()
    {
        if (!IsOpen)
        {
            return;
        }

        Close();

        EmitSignal(SignalName.Kept);
    }

    public void Close()
    {
        IsOpen = false;
    }

    private bool IsAnswerable()
    {
        return IsOpen && IsVisibleInTree();
    }

    private void LinkButtons()
    {
        var (left, right) = ConfirmButton.GetIndex() < KeepButton.GetIndex()
            ? (ConfirmButton, KeepButton)
            : (KeepButton, ConfirmButton);

        left.FocusNeighborRight = left.GetPathTo(right);
        left.FocusNext = left.GetPathTo(right);
        right.FocusNeighborLeft = right.GetPathTo(left);
        right.FocusPrevious = right.GetPathTo(left);
    }

    private void Confirm()
    {
        if (!IsOpen)
        {
            return;
        }

        Close();

        EmitSignal(SignalName.Confirmed);
    }
}
