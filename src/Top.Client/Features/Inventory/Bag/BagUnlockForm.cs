using Godot;
using Top.Client.Features.Lobby.Select.Widgets;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Features.Inventory.Bag;

public partial class BagUnlockForm : PanelContainer
{
    private const string WrongCodeError = "Wrong security code";

    private static readonly string LengthError =
        $"Use {SecurityCode.ShortestLength} to {SecurityCode.LongestLength} digits";

    [Signal]
    public delegate void UnlockRequestedEventHandler(string code);

    [Signal]
    public delegate void CodeRejectedEventHandler(string error);

    [Export] public SecurityCodeEdit CodeEdit { get; set; } = null!;

    [Export] public Button UnlockButton { get; set; } = null!;

    public bool IsWaiting { get; private set; }

    public override void _Ready()
    {
        CodeEdit.TextChanged += _ => UpdateUnlockButton();
        CodeEdit.TextSubmitted += _ => Unlock();

        UnlockButton.Pressed += Unlock;
    }

    public void Clear()
    {
        CodeEdit.Text = string.Empty;

        UpdateUnlockButton();
    }

    public void FocusCode()
    {
        if (IsVisibleInTree() && !IsWaiting)
        {
            CodeEdit.Edit();
        }
    }

    public void ShowWaiting(bool isWaiting)
    {
        IsWaiting = isWaiting;

        CodeEdit.Editable = !isWaiting;

        UpdateUnlockButton();
    }

    public void RejectCode()
    {
        RejectCode(WrongCodeError);
    }

    private void RejectCode(string error)
    {
        Clear();
        FocusCode();
        EmitSignal(SignalName.CodeRejected, error);
    }

    private void Unlock()
    {
        if (IsWaiting || CodeEdit.Text.Length == 0)
        {
            return;
        }

        if (!SecurityCode.IsValid(CodeEdit.Text))
        {
            RejectCode(LengthError);

            return;
        }

        EmitSignal(SignalName.UnlockRequested, CodeEdit.Text);
    }

    private void UpdateUnlockButton()
    {
        UnlockButton.Disabled = IsWaiting || CodeEdit.Text.Length == 0;
    }
}
