using System.Linq;
using Godot;
using Top.Client.Features.Lobby.Select.Widgets;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Features.Lobby.Select.Forms;

public partial class SecurityCodeForm : VBoxContainer
{
    private static readonly string LengthError =
        $"Use {SecurityCode.ShortestLength} to {SecurityCode.LongestLength} digits";

    private const string MismatchError = "New codes don't match";

    private LineEdit[] _fields = [];

    [Signal]
    public delegate void CreateRequestedEventHandler(string code);

    [Signal]
    public delegate void ChangeRequestedEventHandler(string currentCode, string newCode);

    [Signal]
    public delegate void LeaveRequestedEventHandler();

    [Export] public bool AsksCurrentCode { get; set; }

    [Export] public Control CurrentCodeField { get; set; } = null!;

    [Export] public SecurityCodeEdit CurrentCodeEdit { get; set; } = null!;

    [Export] public SecurityCodeEdit NewCodeEdit { get; set; } = null!;

    [Export] public SecurityCodeEdit RepeatCodeEdit { get; set; } = null!;

    [Export] public Label ErrorLabel { get; set; } = null!;

    [Export] public Button LeaveButton { get; set; } = null!;

    [Export] public Button SaveButton { get; set; } = null!;

    public bool IsWaiting { get; private set; }

    public Control FirstField => _fields[0];

    public override void _Ready()
    {
        CurrentCodeField.Visible = AsksCurrentCode;

        _fields = AsksCurrentCode
            ? [CurrentCodeEdit, NewCodeEdit, RepeatCodeEdit]
            : [NewCodeEdit, RepeatCodeEdit];

        foreach (var edit in _fields)
        {
            edit.TextChanged += _ => ShowChange();
        }

        for (var index = 0; index < _fields.Length - 1; index++)
        {
            var next = _fields[index + 1];

            _fields[index].TextSubmitted += _ => next.Edit();
        }

        _fields[^1].TextSubmitted += _ => Save();

        SaveButton.Pressed += Save;
        LeaveButton.Pressed += () => EmitSignal(SignalName.LeaveRequested);

        LinkFocus();
    }

    public void Open()
    {
        foreach (var edit in _fields)
        {
            edit.Text = string.Empty;
        }

        HideError();
        ShowWaiting(false);

        _fields[0].Edit();
    }

    public void ShowWaiting(bool isWaiting)
    {
        IsWaiting = isWaiting;

        foreach (var edit in _fields)
        {
            edit.Editable = !isWaiting;
        }

        LeaveButton.Disabled = isWaiting;

        UpdateSaveButton();

        if (!isWaiting && GetViewport().GuiGetFocusOwner() is LineEdit focused && _fields.Contains(focused))
        {
            focused.Edit();
        }
    }

    public void ShowError(string text)
    {
        ErrorLabel.Text = text;
        ErrorLabel.Visible = true;
    }

    public void RejectCurrentCode(string error)
    {
        CurrentCodeEdit.Text = string.Empty;

        ShowError(error);
        UpdateSaveButton();

        CurrentCodeEdit.Edit();
    }

    public void LinkAbove(Control upper)
    {
        foreach (var field in _fields)
        {
            field.FocusNeighborTop = field.GetPathTo(upper);
        }
    }

    private bool IsFilled()
    {
        return _fields.All(edit => edit.Text.Length > 0);
    }

    private void Save()
    {
        if (IsWaiting || !IsFilled())
        {
            return;
        }

        var code = NewCodeEdit.Text;

        if (!SecurityCode.IsValid(code))
        {
            RejectCodes(LengthError);
        }
        else if (RepeatCodeEdit.Text != code)
        {
            RejectCodes(MismatchError);
        }
        else if (AsksCurrentCode)
        {
            EmitSignal(SignalName.ChangeRequested, CurrentCodeEdit.Text, code);
        }
        else
        {
            EmitSignal(SignalName.CreateRequested, code);
        }
    }

    private void RejectCodes(string error)
    {
        NewCodeEdit.Text = string.Empty;
        RepeatCodeEdit.Text = string.Empty;

        ShowError(error);
        UpdateSaveButton();

        NewCodeEdit.Edit();
    }

    private void ShowChange()
    {
        HideError();
        UpdateSaveButton();
    }

    private void HideError()
    {
        ErrorLabel.Visible = false;
    }

    private void UpdateSaveButton()
    {
        SaveButton.Disabled = IsWaiting || !IsFilled();
    }

    private void LinkFocus()
    {
        for (var index = 0; index < _fields.Length - 1; index++)
        {
            LinkSideBySide(_fields[index], _fields[index + 1]);
        }

        LinkSideBySide(LeaveButton, SaveButton);

        foreach (var edit in _fields[..^1])
        {
            edit.FocusNeighborBottom = edit.GetPathTo(LeaveButton);
        }

        LeaveButton.FocusNeighborTop = LeaveButton.GetPathTo(_fields[0]);
        LinkOver(_fields[^1], SaveButton);

        _fields[^1].FocusNext = _fields[^1].GetPathTo(LeaveButton);
        LeaveButton.FocusPrevious = LeaveButton.GetPathTo(_fields[^1]);
    }

    private static void LinkSideBySide(Control left, Control right)
    {
        left.FocusNeighborRight = left.GetPathTo(right);
        right.FocusNeighborLeft = right.GetPathTo(left);
        left.FocusNext = left.GetPathTo(right);
        right.FocusPrevious = right.GetPathTo(left);
    }

    private static void LinkOver(Control upper, Control lower)
    {
        upper.FocusNeighborBottom = upper.GetPathTo(lower);
        lower.FocusNeighborTop = lower.GetPathTo(upper);
    }
}
