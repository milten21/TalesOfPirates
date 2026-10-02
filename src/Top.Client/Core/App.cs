using System.IO;
using System.Linq;
using Godot;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.LegacyBridge;
using Top.Client.Messages;
using Top.Client.Messages.Events;
using Top.Client.Tools.Debug;
using Top.Client.UI;
using Top.Logging;

namespace Top.Client.Core;

public partial class App : Node
{
    private const string ServerFilePath = "user://servers.json";

    private const string ContentFolderSetting = "top/content/folder";

    private const string DebugModeArgument = "--debug-mode";

    private ServerConnection _connection = null!;

    private Session _session = null!;

    private SceneRouter _router = null!;

    private ConnectionWatch _connectionWatch = null!;

    private DebugOverlay? _debugOverlay;

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

        _router = new SceneRouter(GetTree(), ThemeLibrary.Load(ThemeName), ServerStore, _session, _connection,
            Content);

        _connectionWatch = new ConnectionWatch(_session, _router);

        _connection.Subscribe<DisconnectedEvent>(_connectionWatch.ReportLostConnection);

        _router.SetupCurrentScene();
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
