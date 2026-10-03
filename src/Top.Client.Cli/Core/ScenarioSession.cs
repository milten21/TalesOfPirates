using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Top.Client.Messages;
using Top.Client.Messages.Events;

namespace Top.Client.Cli.Core;

public class ScenarioSession : IDisposable
{
    public static readonly TimeSpan DefaultTimeLimit = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan DrainInterval = TimeSpan.FromMilliseconds(20);

    private readonly IServerConnection _connection;

    private readonly List<IDisposable> _subscriptions = [];

    private readonly Dictionary<Type, object> _states = [];

    private DisconnectedEvent? _disconnection;

    public ScenarioSession(IServerConnection connection, ServerAccount account)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Account = account ?? throw new ArgumentNullException(nameof(account));

        LogEveryEvent();
        Subscribe<DisconnectedEvent>(disconnected => _disconnection = disconnected);
    }

    public ServerAccount Account { get; }

    public EventLog Events { get; } = new();

    public bool IsOpen => _connection.IsOpen;

    public event Action<IServerEvent>? EventArrived;

    public void AddState<TState>(TState state) where TState : class
    {
        _states.Add(typeof(TState), state ?? throw new ArgumentNullException(nameof(state)));
    }

    public TState GetState<TState>() where TState : class
    {
        return _states.TryGetValue(typeof(TState), out var state)
            ? (TState)state
            : throw new InvalidOperationException($"the session has no {typeof(TState).Name}");
    }

    public void Open()
    {
        Events.Clear();
        _disconnection = null;

        _connection.Connect(Account.Host, Account.Port, isEncrypted: true);
    }

    public void Close()
    {
        _connection.Close();
    }

    public void Send(IServerCommand command)
    {
        _connection.Send(command);
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IServerEvent
    {
        _subscriptions.Add(_connection.Subscribe(handler));
    }

    public void Drain()
    {
        _connection.Drain();
    }

    public void DrainFor(TimeSpan duration, string description)
    {
        var stopwatch = Stopwatch.StartNew();

        for (var remaining = duration; remaining > TimeSpan.Zero; remaining = duration - stopwatch.Elapsed)
        {
            Thread.Sleep(remaining < DrainInterval ? remaining : DrainInterval);

            _connection.Drain();

            FailIfLost($"during {description}");
        }
    }

    public void WaitUntil(TimeSpan timeLimit, string description, Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            _connection.Drain();

            if (condition())
            {
                return;
            }

            FailIfLost($"while waiting for {description}");

            if (stopwatch.Elapsed >= timeLimit)
            {
                throw new StepFailedException($"no {description} in {ValueText.FromSeconds(timeLimit)}");
            }

            Thread.Sleep(DrainInterval);
        }
    }

    public TDone WaitForReply<TDone, TFailed>(TimeSpan timeLimit, string description,
        Func<TFailed, string> describeFailure, Func<TDone, bool>? isDone = null, Func<TFailed, bool>? isFailed = null)
        where TDone : IServerEvent where TFailed : IServerEvent
    {
        IServerEvent? reply = null;

        WaitUntil(timeLimit, description, () => (reply = Events.Take(serverEvent =>
            (serverEvent is TDone done && (isDone?.Invoke(done) ?? true)) ||
            (serverEvent is TFailed failed && (isFailed?.Invoke(failed) ?? true)))) != null);

        return reply is TFailed refused ? throw new StepFailedException(describeFailure(refused)) : (TDone)reply!;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();

        _connection.Close();
    }

    private void FailIfLost(string when)
    {
        if (_disconnection != null)
        {
            throw new StepFailedException($"the connection was lost ({_disconnection.Reason}) {when}");
        }
    }

    private void LogEveryEvent()
    {
        var subscribe = typeof(ScenarioSession).GetMethod(nameof(LogEvents),
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        foreach (var eventType in ServerEventType.All)
        {
            subscribe.MakeGenericMethod(eventType).Invoke(this, null);
        }
    }

    private void LogEvents<TEvent>() where TEvent : IServerEvent
    {
        Subscribe<TEvent>(serverEvent =>
        {
            Events.Add(serverEvent);
            EventArrived?.Invoke(serverEvent);
        });
    }
}
