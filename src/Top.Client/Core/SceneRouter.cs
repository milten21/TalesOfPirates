using System;
using Godot;
using Top.Client.Features.Lobby.Select;
using Top.Client.Features.MainMenu;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.Features.World;
using Top.Client.Messages;
using Top.Client.Tools;
using Top.Client.UI.Cursors;
using Top.Logging;

namespace Top.Client.Core;

public class SceneRouter(
    SceneTree tree,
    Theme? theme,
    ServerStore serverStore,
    Session session,
    IServerConnection connection,
    ServerContent content,
    WorldCursorService worldCursorService)
{
    private const string ThemeRootGroup = "theme_root";

    private const string MainMenuScenePath = "res://Features/MainMenu/MainMenuScene.tscn";

    private const string CharacterSelectScenePath = "res://Features/Lobby/Select/CharacterSelectScene.tscn";

    private const string WorldScenePath = "res://Features/World/WorldScene.tscn";

    public void SetupCurrentScene()
    {
        Node? scene = tree.CurrentScene;

        if (scene == null)
        {
            return;
        }

        switch (scene)
        {
            case MainMenuScene mainMenu:
                SetupMainMenu(mainMenu, new MainMenuPayload());
                break;
            case MapViewer viewer:
                viewer.Setup(content);
                break;
        }

        ApplyTheme(scene);
    }

    public void GoToMainMenu(MainMenuPayload payload)
    {
        GoTo<MainMenuScene>(MainMenuScenePath, scene => SetupMainMenu(scene, payload));
    }

    public void GoToCharacterSelect(CharacterSelectPayload payload)
    {
        GoTo<CharacterSelectScene>(CharacterSelectScenePath,
            scene => scene.Setup(session, connection, content, this, payload));
    }

    public void GoToWorld(WorldPayload payload)
    {
        GoTo<WorldScene>(WorldScenePath,
            scene => scene.Setup(session, connection, content, this, payload, worldCursorService));
    }

    public void LeaveServer()
    {
        ForgetLogin();

        connection.Close();

        GoToMainMenu(new MainMenuPayload());
    }

    public void LeaveServer(string lostConnectionText)
    {
        ForgetLogin();

        connection.CloseAsLost();

        Callable.From(() => GoToMainMenu(new MainMenuPayload(lostConnectionText))).CallDeferred();
    }

    private static Control? FindThemeRoot(Node node)
    {
        if (node is Control control && control.IsInGroup(ThemeRootGroup))
        {
            return control;
        }

        foreach (Node child in node.GetChildren())
        {
            if (FindThemeRoot(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void ForgetLogin()
    {
        session.CurrentAccountName = null;
        session.CurrentLogin = null;
    }

    private void SetupMainMenu(MainMenuScene scene, MainMenuPayload payload)
    {
        scene.Setup(serverStore, session, connection, this, payload);
    }

    private void GoTo<TScene>(string path, Action<TScene> setup) where TScene : Node
    {
        if (GD.Load<PackedScene>(path)?.Instantiate() is not TScene scene)
        {
            Log.Error($"the scene at {path} did not open as a {typeof(TScene).Name}");

            return;
        }

        setup(scene);

        ApplyTheme(scene);

        Error error = tree.ChangeSceneToNode(scene);

        if (error != Error.Ok)
        {
            Log.Error($"the scene at {path} did not become the current scene ({error})");

            scene.QueueFree();
        }
    }

    private void ApplyTheme(Node scene)
    {
        if (theme != null && FindThemeRoot(scene) is { } root)
        {
            root.Theme = theme;
        }
    }
}
