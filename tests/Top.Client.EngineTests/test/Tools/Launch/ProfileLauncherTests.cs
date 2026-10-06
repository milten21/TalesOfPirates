using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Top.Client.Core;
using Top.Client.Features.MainMenu;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.Features.World;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Data;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Client.Tools.Launch;
using Top.Client.UI;
using Top.Client.UI.Cursors;
using static GdUnit4.Assertions;

namespace Top.Client.EngineTests.Test.Tools.Launch;

[TestSuite]
[RequireGodotRuntime]
public class ProfileLauncherTests
{
    private const string Host = "127.0.0.1";

    private const int Port = 1973;

    private const string Account = "tester";

    private const string Password = "password";

    private const string CharacterName = "Hero";

    private const string MapName = "garner";

    private const int SceneChangeFrames = 2;

    private SceneTree _tree = null!;

    private Node _runnerScene = null!;

    private FakeServerConnection _connection = null!;

    private Session _session = null!;

    private readonly LaunchProfile _profile = new LaunchProfile
    {
        Host = Host,
        Port = Port,
        Account = Account,
        Password = Password,
        CharacterName = CharacterName
    };

    private ProfileLauncher _launcher = null!;

    [BeforeTest]
    public void StartFromEmptyScene()
    {
        _tree = (SceneTree)Engine.GetMainLoop();
        _runnerScene = _tree.CurrentScene;

        var emptyScene = new Node();

        _tree.Root.AddChild(emptyScene);
        _tree.CurrentScene = emptyScene;

        _connection = new FakeServerConnection();

        _session = new Session();
        var serverStore = new ServerStore(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        var router = new SceneRouter(_tree, serverStore, _session, _connection,
            TestContent.ServerContent, new WorldCursorService(new CursorSkin(ThemePaths.BaseFolder)));

        _launcher = new ProfileLauncher(_connection, _session, router);
    }

    [AfterTest]
    public async Task RestoreRunnerScene()
    {
        await WaitForSceneChange();

        if (_tree.CurrentScene != _runnerScene)
        {
            _tree.CurrentScene?.Free();
        }

        _tree.CurrentScene = _runnerScene;
    }

    [TestCase]
    public void Launch_connects_and_logs_in()
    {
        _launcher.Launch(_profile);

        AssertThat(_connection.Host).IsEqual(Host);
        AssertThat(_connection.Port).IsEqual(Port);

        var logIn = _connection.Sent.OfType<LogInCommand>().Single();

        AssertThat(logIn.Account).IsEqual(Account);
        AssertThat(logIn.Password).IsEqual(Password);
    }

    [TestCase]
    public void Login_reply_begins_play_with_index_of_named_character()
    {
        _launcher.Launch(_profile);

        _connection.Publish(CreateLoginReply("Alpha", "Beta", CharacterName));

        AssertThat(_connection.Sent.OfType<BeginPlayCommand>().Single().CharacterIndex).IsEqual(2);
    }

    [TestCase]
    public async Task Entered_map_makes_world_scene_current_with_login_in_session()
    {
        var loginReply = CreateLoginReply(CharacterName);

        _launcher.Launch(_profile);

        _connection.Publish(loginReply);
        _connection.Publish(CreateEnteredMap());

        await WaitForSceneChange();

        AssertThat(_tree.CurrentScene).IsInstanceOf<WorldScene>();
        AssertThat(_session.CurrentAccountName).IsEqual(Account);
        AssertThat(_session.CurrentLogin).IsSame(loginReply);
    }

    [TestCase]
    public async Task Refused_login_shows_main_menu()
    {
        _launcher.Launch(_profile);

        _connection.Publish(new LoginFailedEvent(1104, "Wrong password"));

        await AssertShowsMainMenu();
    }

    [TestCase]
    public async Task Missing_character_shows_main_menu()
    {
        _launcher.Launch(_profile);

        _connection.Publish(CreateLoginReply("Alpha", "Beta"));

        AssertThat(_connection.Sent.OfType<BeginPlayCommand>()).IsEmpty();

        await AssertShowsMainMenu();
    }

    [TestCase]
    public async Task Refused_begin_play_shows_main_menu()
    {
        _launcher.Launch(_profile);

        _connection.Publish(CreateLoginReply(CharacterName));
        _connection.Publish(new BeginPlayFailedEvent(1, "Refused"));

        await AssertShowsMainMenu();
    }

    [TestCase]
    public async Task Lost_connection_shows_main_menu()
    {
        _launcher.Launch(_profile);

        _connection.Publish(new DisconnectedEvent(DisconnectReason.Unreachable));

        await AssertShowsMainMenu();
    }

    [TestCase]
    public void Launch_while_launching_sends_nothing()
    {
        _launcher.Launch(_profile);
        _launcher.Launch(_profile);

        AssertThat(_connection.Sent.OfType<LogInCommand>().Count()).IsEqual(1);
    }

    [TestCase]
    public async Task Launch_after_failure_logs_in_again()
    {
        _launcher.Launch(_profile);

        _connection.Publish(new LoginFailedEvent(1104, "Wrong password"));

        await WaitForSceneChange();

        _launcher.Launch(_profile);

        AssertThat(_connection.Sent.OfType<LogInCommand>().Count()).IsEqual(2);
    }

    private static LoginSucceededEvent CreateLoginReply(params string[] characterNames)
    {
        var characters = characterNames
            .Select(name => new CharacterSlot(name, "Newbie", 1, 1, new CharacterLook(0, [])));

        return new LoginSucceededEvent(characters, 3, hasSecurityCode: false);
    }

    private static MapEnteredEvent CreateEnteredMap()
    {
        var character = new CharacterState(1, CharacterControlType.Player, 1, CharacterName, System.Numerics.Vector2.Zero, 0, 0, 0,
            0, string.Empty, string.Empty, 0, 1, 100, 100, 100, 100, 0, 0, 0, 0, 0, 4.8f, null, null);

        return new MapEnteredEvent(MapName, EnterType.Edge, character, new BagSync(null, []), isBagLocked: false, []);
    }

    private async Task AssertShowsMainMenu()
    {
        await WaitForSceneChange();

        AssertThat(_tree.CurrentScene).IsInstanceOf<MainMenuScene>();
        AssertThat(_connection.IsOpen).IsFalse();
    }

    private async Task WaitForSceneChange()
    {
        for (var frame = 0; frame < SceneChangeFrames; frame++)
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
