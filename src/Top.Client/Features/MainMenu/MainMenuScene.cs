using System;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Lobby.Select;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.Messages;
using Top.Client.UI;
using Top.Logging;

namespace Top.Client.Features.MainMenu;

public partial class MainMenuScene : Node
{
    private const double FadeSeconds = 0.15;

    private ServerStore _serverStore = null!;

    private Session _session = null!;

    private IServerConnection _connection = null!;

    private SceneRouter _router = null!;

    private MainMenuPayload _payload = null!;

    private Screen? _screen;

    private Tween? _fade;

    [Export] public Control ScreenSlot { get; set; } = null!;

    [Export] public Control SmallWordmark { get; set; } = null!;

    [Export] public MessagePopup MessagePopup { get; set; } = null!;

    [Export] public ConfirmPopup ExitPopup { get; set; } = null!;

    [Export] public PackedScene MainMenuScreen { get; set; } = null!;

    [Export] public PackedScene ServersScreen { get; set; } = null!;

    [Export] public PackedScene AccountsScreen { get; set; } = null!;

    [Export] public PackedScene LoginScreen { get; set; } = null!;

    public void Setup(ServerStore serverStore, Session session, IServerConnection connection,
        SceneRouter router, MainMenuPayload payload)
    {
        _serverStore = serverStore;
        _session = session;
        _connection = connection;
        _router = router;
        _payload = payload;
    }

    public override void _Ready()
    {
        ExitPopup.Confirmed += Quit;

        ShowStart();

        if (_payload.LostConnectionText is { } text)
        {
            MessagePopup.Open(Disconnection.Title, text);
        }
    }

    private void ShowStart()
    {
        if (_connection.IsLost)
        {
            ShowServers();

            return;
        }

        if (_session.CurrentServer is not { } server)
        {
            ShowMenu();

            return;
        }

        ShowLogin(server, FindAccountNameToFill(server));
    }

    private void ShowMenu()
    {
        ShowScreen<Menu.MainMenuScreen>(MainMenuScreen, showsWordmark: false, screen =>
        {
            screen.Setup(_serverStore.LastServer);

            screen.ContinuePressed += Continue;
            screen.OnlinePressed += ShowServers;
            screen.ExitPressed += AskToQuit;
        });
    }

    private void Continue()
    {
        if (_serverStore.LastServer is not { LastAccount: { } account } server)
        {
            return;
        }

        _session.CurrentServer = server;

        ShowLogin(server, account.Name);
    }

    private void LeaveLogin(ServerEntry server)
    {
        if (HasAccountsStep(server))
        {
            ShowAccounts(server);
        }
        else
        {
            ShowServers();
        }
    }

    private void ShowServers()
    {
        ShowScreen<ServersScreen>(ServersScreen, showsWordmark: true, screen =>
        {
            screen.Setup(_serverStore);

            screen.Closed += ShowMenu;
            screen.ServerChosen += () => ChooseServer(screen);
        });
    }

    private void ChooseServer(ServersScreen screen)
    {
        if (screen.ChosenServer is not { } server)
        {
            return;
        }

        _session.CurrentServer = server;

        if (HasAccountsStep(server))
        {
            ShowAccounts(server);
        }
        else
        {
            ShowLogin(server, FindAccountNameToFill(server));
        }
    }

    private static bool HasAccountsStep(ServerEntry server)
    {
        return server.Accounts.Count > 1;
    }

    private static string FindAccountNameToFill(ServerEntry server)
    {
        if (server.LastAccount is { } lastAccount)
        {
            return lastAccount.Name;
        }

        return server.Accounts.Count == 1 ? server.Accounts[0].Name : string.Empty;
    }

    private void ShowAccounts(ServerEntry server)
    {
        ShowScreen<Accounts.AccountsScreen>(AccountsScreen, showsWordmark: true, screen =>
        {
            screen.Setup(_serverStore, server);

            screen.Closed += ShowServers;
            screen.AccountChosen += name => ShowLogin(server, name);
            screen.NewAccountChosen += () => ShowLogin(server, string.Empty);
        });
    }

    private void ShowLogin(ServerEntry server, string accountName)
    {
        ShowScreen<Login.LoginScreen>(LoginScreen, showsWordmark: true, screen =>
        {
            screen.Setup(_connection, server, accountName);

            screen.Closed += () => LeaveLogin(server);
            screen.LoggedIn += () => CompleteLogin(server, screen);
        });
    }

    private void CompleteLogin(ServerEntry server, Login.LoginScreen screen)
    {
        if (screen.Login is not { } login)
        {
            return;
        }

        _serverStore.RememberLogin(server, screen.AccountName, DateTime.UtcNow);

        _session.CurrentAccountName = screen.AccountName;
        _session.CurrentLogin = login;

        _router.GoToCharacterSelect(new CharacterSelectPayload(login.Characters, login.SlotCap, login.HasSecurityCode));
    }

    private void AskToQuit()
    {
        ExitPopup.Open("Exit game", "Close Tales of Pirates and return to the desktop?", "Exit", "Cancel",
            isDestructive: false);
    }

    private void Quit()
    {
        GetTree().Quit();
    }

    private void ShowScreen<TScreen>(PackedScene packed, bool showsWordmark, Action<TScreen> setup)
        where TScreen : Screen
    {
        if (packed?.Instantiate() is not TScreen screen)
        {
            Log.Error($"the main menu was handed a scene that is not a {typeof(TScreen).Name}");

            return;
        }

        setup(screen);

        GetViewport().GuiReleaseFocus();

        _fade?.Kill();

        FreeLeavingScreens();

        var leaving = _screen;

        _screen = screen;

        screen.Modulate = Colors.Transparent;

        ScreenSlot.AddChild(screen);

        _fade = CreateTween().SetParallel();

        _fade.TweenProperty(screen, "modulate:a", 1f, FadeSeconds);
        _fade.TweenProperty(SmallWordmark, "modulate:a", showsWordmark ? 1f : 0f, FadeSeconds);

        if (leaving is null)
        {
            return;
        }

        leaving.ProcessMode = ProcessModeEnum.Disabled;

        _fade.TweenProperty(leaving, "modulate:a", 0f, FadeSeconds);
        _fade.Chain().TweenCallback(Callable.From(leaving.QueueFree));
    }

    private void FreeLeavingScreens()
    {
        foreach (var child in ScreenSlot.GetChildren())
        {
            if (child != _screen)
            {
                child.QueueFree();
            }
        }
    }
}
