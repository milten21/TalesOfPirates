using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;
using Top.Client.Messages.Events;

namespace Top.Client.Cli.Tests;

public class FakeServerConnection : IServerConnection
{
    private readonly List<Subscription> _subscriptions = [];

    private readonly List<IServerCommand> _sent = [];

    private readonly Queue<IServerEvent> _pendingEvents = new();

    private readonly List<(int DrainCount, IServerEvent Event)> _delayedEvents = [];

    private int _drainCount;

    private readonly Dictionary<Type, Func<IServerCommand, IServerEvent[]>> _replies = [];

    public bool IsOpen { get; private set; }

    public bool IsLost { get; private set; }

    public string Host { get; private set; }

    public int Port { get; private set; }

    public IReadOnlyList<IServerCommand> Sent => _sent;

    public void Connect(string host, int port, bool isEncrypted)
    {
        Host = host;
        Port = port;
        IsOpen = true;
        IsLost = false;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void CloseAsLost()
    {
        IsLost = true;

        Close();
    }

    public void PauseDrain()
    {
    }

    public void ResumeDrain()
    {
    }

    public void Drain()
    {
        _drainCount++;

        foreach (var delayed in _delayedEvents.Where(delayed => delayed.DrainCount <= _drainCount))
        {
            _pendingEvents.Enqueue(delayed.Event);
        }

        _delayedEvents.RemoveAll(delayed => delayed.DrainCount <= _drainCount);

        while (_pendingEvents.TryDequeue(out var serverEvent))
        {
            if (serverEvent is DisconnectedEvent)
            {
                IsOpen = false;
            }

            foreach (var subscription in _subscriptions.ToList())
            {
                subscription.Handle(serverEvent);
            }
        }
    }

    public void Send(IServerCommand command)
    {
        _sent.Add(command);

        if (_replies.TryGetValue(command.GetType(), out var reply))
        {
            foreach (var serverEvent in reply(command))
            {
                _pendingEvents.Enqueue(serverEvent);
            }
        }
    }

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IServerEvent
    {
        var subscription = new Subscription(this, serverEvent =>
        {
            if (serverEvent is TEvent typedEvent)
            {
                handler(typedEvent);
            }
        });

        _subscriptions.Add(subscription);

        return subscription;
    }

    public void Queue(IServerEvent serverEvent, int afterDrains = 0)
    {
        _delayedEvents.Add((_drainCount + afterDrains, serverEvent));
    }

    public void ReplyTo<TCommand>(Func<TCommand, IServerEvent[]> reply) where TCommand : IServerCommand
    {
        _replies[typeof(TCommand)] = command => reply((TCommand)command);
    }

    private class Subscription(FakeServerConnection connection, Action<IServerEvent> handle) : IDisposable
    {
        public void Handle(IServerEvent serverEvent)
        {
            handle(serverEvent);
        }

        public void Dispose()
        {
            connection._subscriptions.Remove(this);
        }
    }
}
