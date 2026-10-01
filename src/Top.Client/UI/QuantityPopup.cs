using System.Globalization;
using System.Linq;
using Godot;

namespace Top.Client.UI;

public partial class QuantityPopup : PopupBase
{
    private static readonly StringName GoldPillVariation = "GoldPill";

    private static readonly StringName DangerPillVariation = "DangerPill";

    private int _maximum;

    [Signal]
    public delegate void ConfirmedEventHandler(int quantity);

    [Export] public Label TitleLabel { get; set; } = null!;

    [Export] public Label MessageLabel { get; set; } = null!;

    [Export] public LineEdit QuantityEdit { get; set; } = null!;

    [Export] public Button ConfirmButton { get; set; } = null!;

    [Export] public Button CancelButton { get; set; } = null!;

    protected override Control? FirstFocus => QuantityEdit;

    public override void _Ready()
    {
        base._Ready();

        QuantityEdit.TextChanged += KeepQuantity;
        QuantityEdit.TextSubmitted += _ => Confirm();
        QuantityEdit.GuiInput += CloseOnCancel;
        ConfirmButton.Pressed += Confirm;
        CancelButton.Pressed += Close;
    }

    public void Open(string title, string message, string confirmText, int initial, int maximum, bool isDestructive)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        ConfirmButton.Text = confirmText.ToUpperInvariant();
        ConfirmButton.ThemeTypeVariation = isDestructive ? DangerPillVariation : GoldPillVariation;

        _maximum = maximum;
        QuantityEdit.Text = initial.ToString(CultureInfo.InvariantCulture);

        UpdateConfirmButton();
        OpenFullScreen();
    }

    protected override void Accept()
    {
        Confirm();
    }

    private void KeepQuantity(string text)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length > 0 && (!TryParse(digits, out var quantity) || quantity > _maximum))
        {
            digits = _maximum.ToString(CultureInfo.InvariantCulture);
        }

        if (digits != text)
        {
            QuantityEdit.Text = digits;
            QuantityEdit.CaretColumn = digits.Length;
        }

        UpdateConfirmButton();
    }

    private void CloseOnCancel(InputEvent received)
    {
        if (received.IsActionPressed("ui_cancel"))
        {
            QuantityEdit.AcceptEvent();
            Close();
        }
    }

    private void Confirm()
    {
        if (IsClosing || !TryReadQuantity(out var quantity))
        {
            return;
        }

        Close();

        EmitSignal(SignalName.Confirmed, quantity);
    }

    private void UpdateConfirmButton()
    {
        ConfirmButton.Disabled = !TryReadQuantity(out _);
    }

    private bool TryReadQuantity(out int quantity)
    {
        return TryParse(QuantityEdit.Text, out quantity) && quantity >= 1 && quantity <= _maximum;
    }

    private static bool TryParse(string text, out int quantity)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out quantity);
    }
}
