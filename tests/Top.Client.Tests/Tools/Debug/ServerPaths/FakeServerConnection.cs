using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;

namespace Top.Client.Tests.Tools.Debug.ServerPaths;

public class FakeServerConnection : IServerConnection
{
    private readonly List<Subscription> _subscriptions = [];

    public bool IsOpen => true;

    public bool IsLost => false;

    public int SubscriptionCount => _subscriptions.Count;

    public void Connect(string host, int port, bool isEncrypted)
    {
    }

    public void Close()
    {
    }

    public void CloseAsLost()
    {
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
