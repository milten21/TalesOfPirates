using System;
using System.IO;
using System.Linq;
using Godot;
using Top.Client.Features.MainMenu;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.LegacyBridge;
using Top.Client.Messages;
using Top.Client.Messages.Events;
using Top.Client.Tools.Debug;
using Top.Client.Tools.Launch;
using Top.Client.UI;
using Top.Client.UI.Cursors;
using Top.Logging;

namespace Top.Client.Core;

public partial class App : Node
{
    private const string ServerFilePath = "user://servers.json";

    private const string LaunchProfileFolderPath = "user://launch_profiles";

    private const string ContentFolderSetting = "top/content/folder";

    private const string DebugModeArgument = "--debug-mode";

    private const string LaunchProfileArgument = "--launch-profile";

    private ServerConnection _connection = null!;

    private Session _session = null!;

    private SceneRouter _router = null!;

    private ConnectionWatch _connectionWatch = null!;

    private DebugOverlay? _debugOverlay;

    private ProfileLauncher? _profileLauncher;

    private CursorSkin _cursorSkin = null!;

    private WorldCursorService _worldCursorService = null!;

    [Export] public string ThemeName { get; set; } = ThemePaths.DefaultName;

    public ServerContent Content { get; private set; } = null!;

    public ServerStore ServerStore { get; private set; } = null!;

    public IServerConnection Connection => _connection;

    public override void _EnterTree()
    {
        Log.Writer ??= new GodotLogWriter();

        if (IsDebugModeOn())
        {
            _debugOverlay = new DebugOverlay();
        }

        _connection = new ServerConnection(_debugOverlay?.PacketLog);

        Content = new ServerContent(ReadContentFolder());

        ServerStore = new ServerStore(ProjectSettings.GlobalizePath(ServerFilePath));

        _session = new Session();

        ProjectTheme.Apply(ThemeLibrary.Load(ThemeName));

        _cursorSkin = new CursorSkin(ThemePaths.BaseFolder);

        _cursorSkin.Reload();

        _worldCursorService = new WorldCursorService(_cursorSkin);

        _worldCursorService.Start(GetTree());

        _router = new SceneRouter(GetTree(), ServerStore, _session, _connection,
            Content, _worldCursorService);

        _connectionWatch = new ConnectionWatch(_session, _router);

        _connection.Subscribe<DisconnectedEvent>(_connectionWatch.ReportLostConnection);

        if (OS.IsDebugBuild())
        {
            _profileLauncher = new ProfileLauncher(_connection, _session, _router);
        }

        _router.SetupCurrentScene();

        if (ReadLaunchProfileArgument() is { } profileName)
        {
            StartProfileLaunch(profileName);
        }
    }

    public override void _Ready()
    {
        GetViewport().GuiFocusChanged += PlaceCaretAtEnd;

        if (_debugOverlay != null)
        {
            _debugOverlay.Setup(_connection);

            AddChild(_debugOverlay);
        }
    }

    public override void _Process(double delta)
    {
        _connection.Drain();
    }

    public override void _ExitTree()
    {
        _connection.Close();
    }

    public void StartProfileLaunch(string profileName)
    {
        if (_profileLauncher == null)
        {
            return;
        }

        if (GetTree().CurrentScene is not MainMenuScene)
        {
            Log.Error($"launch profile {profileName} can start only from the main menu");

            return;
        }

        var path = ProjectSettings.GlobalizePath($"{LaunchProfileFolderPath}/{profileName}.json");

        if (LaunchProfile.Read(path) is not { } profile)
        {
            Log.Error($"launch profile {profileName} could not be read from {path}");

            return;
        }

        _profileLauncher.Launch(profile);
    }

    private static string? ReadLaunchProfileArgument()
    {
        var arguments = OS.GetCmdlineUserArgs();
        var index = Array.IndexOf(arguments, LaunchProfileArgument);

        if (index < 0)
        {
            return null;
        }

        if (index + 1 < arguments.Length)
        {
            return arguments[index + 1];
        }

        Log.Error($"{LaunchProfileArgument} needs a profile name");

        return null;
    }

    private static bool IsDebugModeOn()
    {
        return OS.IsDebugBuild() || OS.GetCmdlineUserArgs().Contains(DebugModeArgument);
    }

    private static string ReadContentFolder()
    {
        var folder = ProjectSettings.GlobalizePath((string)ProjectSettings.GetSettingWithOverride(ContentFolderSetting));

        return Path.Combine(OS.GetExecutablePath().GetBaseDir(), folder);
    }

    private static void PlaceCaretAtEnd(Control control)
    {
        if (control is LineEdit edit)
        {
            edit.CaretColumn = edit.Text.Length;
        }
    }
}
