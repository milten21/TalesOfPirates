using System;
using System.Collections.Generic;
using Top.Client.Messages;

namespace Top.Client.Cli.Core;

public class EventLog
{
    private const int MaximumCount = 10_000;

    private readonly List<IServerEvent> _events = [];

    public void Add(IServerEvent serverEvent)
    {
        _events.Add(serverEvent);

        if (_events.Count > MaximumCount)
        {
            _events.RemoveAt(0);
        }
    }

    public void Clear()
    {
        _events.Clear();
    }

    public IServerEvent? Take(Predicate<IServerEvent> match)
    {
        var index = _events.FindIndex(match);

        return index < 0 ? null : TakeRange(index, index);
    }

    public IServerEvent? TakeWithOlder(Predicate<IServerEvent> match)
    {
        var index = _events.FindIndex(match);

        return index < 0 ? null : TakeRange(0, index);
    }

    public void RemoveAll(Predicate<IServerEvent> match)
    {
        _events.RemoveAll(match);
    }

    public void RemoveThrough(IServerEvent newest, Predicate<IServerEvent> match)
    {
        for (var i = _events.IndexOf(newest); i >= 0; i--)
        {
            if (match(_events[i]))
            {
                _events.RemoveAt(i);
            }
        }
    }

    private IServerEvent TakeRange(int firstIndex, int lastIndex)
    {
        var taken = _events[lastIndex];

        _events.RemoveRange(firstIndex, lastIndex - firstIndex + 1);

        return taken;
    }
}
