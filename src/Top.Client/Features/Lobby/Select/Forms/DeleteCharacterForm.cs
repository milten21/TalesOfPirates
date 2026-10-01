using Godot;
using Top.Client.Features.Lobby.Select.Widgets;

namespace Top.Client.Features.Lobby.Select.Forms;

public partial class DeleteCharacterForm : VBoxContainer
{
    [Signal]
    public delegate void DeleteRequestedEventHandler(string code);

    [Signal]
    public delegate void CancelRequestedEventHandler();

    [Export] public Label TitleLabel { get; set; } = null!;

    [Export] public Label DetailLabel { get; set; } = null!;

    [Export] public SecurityCodeEdit CodeEdit { get; set; } = null!;

    [Export] public Label ErrorLabel { get; set; } = null!;

    [Export] public Button CancelButton { get; set; } = null!;

    [Export] public Button DeleteButton { get; set; } = null!;

    public bool IsWaiting { get; private set; }

    public override void _Ready()
    {
        CodeEdit.TextChanged += _ => ShowChange();
        CodeEdit.TextSubmitted += _ => Delete();

        DeleteButton.Pressed += Delete;
        CancelButton.Pressed += () => EmitSignal(SignalName.CancelRequested);

        LinkFocus();
    }

    public void Open(string title, string detail)
    {
        TitleLabel.Text = title;
        DetailLabel.Text = detail;
        CodeEdit.Text = string.Empty;

        HideError();
        ShowWaiting(false);

        CodeEdit.Edit();
    }

    public void ShowWaiting(bool isWaiting)
    {
        IsWaiting = isWaiting;

        CodeEdit.Editable = !isWaiting;
        CancelButton.Disabled = isWaiting;

        UpdateDeleteButton();

        if (!isWaiting && GetViewport().GuiGetFocusOwner() == CodeEdit)
        {
            CodeEdit.Edit();
        }
    }

    public void ShowError(string text)
    {
        ErrorLabel.Text = text;
        ErrorLabel.Visible = true;
    }

    public void RejectCode(string error)
    {
        CodeEdit.Text = string.Empty;

        ShowError(error);
        UpdateDeleteButton();

        CodeEdit.Edit();
    }

    public void LinkAbove(Control upper)
    {
        CodeEdit.FocusNeighborTop = CodeEdit.GetPathTo(upper);
    }

    private void Delete()
    {
        if (IsWaiting || CodeEdit.Text.Length == 0)
        {
            return;
        }

        EmitSignal(SignalName.DeleteRequested, CodeEdit.Text);
    }

    private void ShowChange()
    {
        HideError();
        UpdateDeleteButton();
    }

    private void HideError()
    {
        ErrorLabel.Visible = false;
    }

    private void UpdateDeleteButton()
    {
        DeleteButton.Disabled = IsWaiting || CodeEdit.Text.Length == 0;
    }

    private void LinkFocus()
    {
        CancelButton.FocusNeighborRight = CancelButton.GetPathTo(DeleteButton);
        DeleteButton.FocusNeighborLeft = DeleteButton.GetPathTo(CancelButton);

        CodeEdit.FocusNeighborBottom = CodeEdit.GetPathTo(CancelButton);
        CancelButton.FocusNeighborTop = CancelButton.GetPathTo(CodeEdit);
        DeleteButton.FocusNeighborTop = DeleteButton.GetPathTo(CodeEdit);

        CodeEdit.FocusNext = CodeEdit.GetPathTo(CancelButton);
        CancelButton.FocusPrevious = CancelButton.GetPathTo(CodeEdit);
        CancelButton.FocusNext = CancelButton.GetPathTo(DeleteButton);
        DeleteButton.FocusPrevious = DeleteButton.GetPathTo(CancelButton);
    }
}
