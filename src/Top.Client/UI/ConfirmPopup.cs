using Godot;

namespace Top.Client.UI;

public partial class ConfirmPopup : PopupBase
{
    private static readonly StringName GoldPillVariation = "GoldPill";

    private static readonly StringName DangerPillVariation = "DangerPill";

    [Signal]
    public delegate void ConfirmedEventHandler();

    [Export] public Label TitleLabel { get; set; } = null!;

    [Export] public Label MessageLabel { get; set; } = null!;

    [Export] public Button ConfirmButton { get; set; } = null!;

    [Export] public Button CancelButton { get; set; } = null!;

    protected override Control? NavigationStart => ConfirmButton;

    public override void _Ready()
    {
        base._Ready();

        ConfirmButton.Pressed += Confirm;
        CancelButton.Pressed += Close;

        ConfirmButton.FocusNeighborLeft = ConfirmButton.GetPathTo(CancelButton);
        ConfirmButton.FocusPrevious = ConfirmButton.GetPathTo(CancelButton);
        CancelButton.FocusNeighborRight = CancelButton.GetPathTo(ConfirmButton);
        CancelButton.FocusNext = CancelButton.GetPathTo(ConfirmButton);
    }

    public void Open(string title, string question, string confirmText, string cancelText, bool isDestructive)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = question;

        ConfirmButton.Text = confirmText.ToUpperInvariant();
        CancelButton.Text = cancelText.ToUpperInvariant();

        ConfirmButton.ThemeTypeVariation = isDestructive ? DangerPillVariation : GoldPillVariation;

        OpenFullScreen();
    }

    protected override void Accept()
    {
        Confirm();
    }

    private void Confirm()
    {
        if (IsClosing)
        {
            return;
        }

        Close();

        EmitSignal(SignalName.Confirmed);
    }
}
