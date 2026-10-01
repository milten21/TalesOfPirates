using Godot;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.UI;

namespace Top.Client.Features.MainMenu.Menu;

public partial class MainMenuScreen : Screen
{
    private ServerEntry? _lastServer;

    [Signal]
    public delegate void ContinuePressedEventHandler();

    [Signal]
    public delegate void OnlinePressedEventHandler();

    [Signal]
    public delegate void ExitPressedEventHandler();

    [Export] public ContinuePill ContinuePill { get; set; } = null!;

    [Export] public MenuItem OnlineItem { get; set; } = null!;

    [Export] public MenuItem SettingsItem { get; set; } = null!;

    [Export] public MenuItem ExitItem { get; set; } = null!;

    protected override Control NavigationStart => ContinuePill.Visible ? ContinuePill : OnlineItem;

    public void Setup(ServerEntry? lastServer)
    {
        _lastServer = lastServer;
    }

    public override void _Ready()
    {
        ContinuePill.Pressed += RaiseContinuePressed;
        OnlineItem.Pressed += RaiseOnlinePressed;
        ExitItem.Pressed += RaiseExitPressed;

        if (_lastServer is { LastAccount: { } account } server)
        {
            ContinuePill.ShowLogin(account.Name, server.Name);

            StackFocus(ContinuePill, OnlineItem, SettingsItem, ExitItem);
        }
        else
        {
            ContinuePill.Visible = false;

            StackFocus(OnlineItem, SettingsItem, ExitItem);
        }
    }

    private void RaiseContinuePressed()
    {
        EmitSignal(SignalName.ContinuePressed);
    }

    private void RaiseOnlinePressed()
    {
        EmitSignal(SignalName.OnlinePressed);
    }

    private void RaiseExitPressed()
    {
        EmitSignal(SignalName.ExitPressed);
    }
}
