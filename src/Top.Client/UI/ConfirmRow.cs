using Godot;

namespace Top.Client.UI;

public partial class ConfirmRow : VBoxContainer
{
    [Signal]
    public delegate void SelectedEventHandler();

    [Signal]
    public delegate void ActivatedEventHandler();

    [Signal]
    public delegate void ConfirmedEventHandler();

    [Export] public ListRow Row { get; set; } = null!;

    [Export] public InlineConfirm InlineConfirm { get; set; } = null!;

    public bool IsConfirming => InlineConfirm.IsOpen;

    public bool WasConfirmOpenedWithKeyboard => InlineConfirm.WasOpenedWithKeyboard;

    public override void _Ready()
    {
        Row.Toggled += EmitSelected;
        Row.Activated += () => EmitSignal(SignalName.Activated);
        InlineConfirm.Confirmed += HideConfirmAndEmitConfirmed;
        InlineConfirm.Kept += HideConfirmAndFocusRow;
    }

    public void Select()
    {
        Row.ButtonPressed = true;
    }

    public void Keep()
    {
        InlineConfirm.Keep();
    }

    public void CloseConfirm()
    {
        InlineConfirm.Close();
        InlineConfirm.Hide();
    }

    protected void Ask(string question)
    {
        Select();

        InlineConfirm.Show();
        InlineConfirm.Ask(question);
    }

    private void HideConfirmAndEmitConfirmed()
    {
        InlineConfirm.Hide();

        EmitSignal(SignalName.Confirmed);
    }

    private void HideConfirmAndFocusRow()
    {
        InlineConfirm.Hide();

        if (WasConfirmOpenedWithKeyboard)
        {
            Row.GrabFocus();
        }
    }

    private void EmitSelected(bool isSelected)
    {
        if (isSelected)
        {
            EmitSignal(SignalName.Selected);
        }
    }
}
