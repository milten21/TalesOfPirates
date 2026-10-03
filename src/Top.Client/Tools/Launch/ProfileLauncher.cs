using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Core;
using Top.Client.Features.MainMenu;
using Top.Client.Features.World;
using Top.Client.Messages;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Events;
using Top.Logging;

namespace Top.Client.Tools.Launch;

public class ProfileLauncher(IServerConnection connection, Session session, SceneRouter router)
{
    private readonly List<IDisposable> _subscriptions = [];

    private LoginSucceededEvent? _loginReply;

    private bool IsLaunching => _subscriptions.Count > 0;

    public void Launch(LaunchProfile profile)
    {
        if (IsLaunching || connection.IsOpen)
        {
            Log.Error($"launch profile for {profile.CharacterName} did not start, a connection is already open");

            return;
        }

        _subscriptions.Add(connection.Subscribe<LoginSucceededEvent>(login => BeginPlay(profile, login)));
        _subscriptions.Add(connection.Subscribe<LoginFailedEvent>(failed =>
            Fail($"the server refused the log in with code {failed.Code}: {failed.Text}")));
        _subscriptions.Add(connection.Subscribe<BeginPlayFailedEvent>(failed =>
            Fail($"the server refused to let {profile.CharacterName} enter with code {failed.Code}: {failed.Text}")));
        _subscriptions.Add(connection.Subscribe<MapEnteredEvent>(entered => GoToWorld(profile, entered)));
        _subscriptions.Add(connection.Subscribe<DisconnectedEvent>(disconnected =>
            Fail($"the connection was lost ({disconnected.Reason})")));

        Log.Info($"launch profile: logging in to {profile.Host}:{profile.Port} as {profile.Account}");

        connection.Connect(profile.Host, profile.Port, isEncrypted: true);
        connection.Send(new LogInCommand(profile.Account, profile.Password));
    }

    private void BeginPlay(LaunchProfile profile, LoginSucceededEvent login)
    {
        var index = login.Characters.ToList().FindIndex(character => character.Name == profile.CharacterName);

        if (index < 0)
        {
            var names = string.Join(", ", login.Characters.Select(character => character.Name));

            Fail($"the account {profile.Account} has no character named {profile.CharacterName}, only: {names}");

            return;
        }

        _loginReply = login;

        connection.Send(new BeginPlayCommand(index));
    }

    private void GoToWorld(LaunchProfile profile, MapEnteredEvent entered)
    {
        var loginReply = _loginReply;

        Stop();

        connection.PauseDrain();

        session.CurrentAccountName = profile.Account;
        session.CurrentLogin = loginReply;

        router.GoToWorld(new WorldPayload(entered));
    }

    private void Fail(string reason)
    {
        Stop();

        connection.Close();

        Log.Error($"launch profile failed: {reason}");

        router.GoToMainMenu(new MainMenuPayload());
    }

    private void Stop()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();

        _loginReply = null;
    }
}
