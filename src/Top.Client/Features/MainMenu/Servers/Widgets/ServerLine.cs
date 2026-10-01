using Godot;

namespace Top.Client.Features.MainMenu.Servers.Widgets;

public partial class ServerLine : MarginContainer
{
    [Export] public Label ServerLabel { get; set; } = null!;

    [Export] public Label AddressLabel { get; set; } = null!;

    [Export] public Label Separator { get; set; } = null!;

    [Export] public Label AccountLabel { get; set; } = null!;

    public void ShowServer(string name, string address)
    {
        ServerLabel.Text = name;
        AddressLabel.Text = address;

        Visible = true;
    }

    public void ShowAccount(string? accountName)
    {
        AccountLabel.Text = accountName ?? string.Empty;

        Separator.Visible = accountName is not null;
        AccountLabel.Visible = accountName is not null;
    }
}
