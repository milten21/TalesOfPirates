using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;

namespace Top.Client.EngineTests.Test;

public class FakeServerConnection : IServerConnection
{
    private readonly List<Subscription> _subscriptions = [];

    private readonly List<IServerCommand> _sent = [];

    public bool IsOpen { get; private set; }

    public bool IsLost { get; private set; }

    public string? Host { get; private set; }

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
    }

    public void Send(IServerCommand command)
    {
        _sent.Add(command);
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

    public void Publish(IServerEvent serverEvent)
    {
        foreach (var subscription in _subscriptions.ToList())
        {
            subscription.Handle(serverEvent);
        }
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
