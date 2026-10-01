using Godot;

namespace Top.Client.UI;

public partial class MessagePopup : PopupBase
{
    [Export] public Label TitleLabel { get; set; } = null!;

    [Export] public Label MessageLabel { get; set; } = null!;

    [Export] public Label CodeLabel { get; set; } = null!;

    [Export] public Button OkButton { get; set; } = null!;

    protected override Control? NavigationStart => OkButton;

    public override void _Ready()
    {
        base._Ready();

        OkButton.Pressed += Close;
    }

    public void Open(string title, string message, int? code = null)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;

        CodeLabel.Text = code is { } value ? $"Error {value}" : string.Empty;
        CodeLabel.Visible = code != null;

        OpenFullScreen();
    }

    protected override void Accept()
    {
        Close();
    }
}
