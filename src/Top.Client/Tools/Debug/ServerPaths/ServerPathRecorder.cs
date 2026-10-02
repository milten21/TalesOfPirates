using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;
using Top.Client.Messages.Play.Events;

namespace Top.Client.Tools.Debug.ServerPaths;

public class ServerPathRecorder(IServerConnection connection, TimeProvider timeProvider)
{
    public static readonly TimeSpan TrailDuration = TimeSpan.FromSeconds(1.5);

    private readonly Dictionary<uint, IReadOnlyList<MoveStep>> _trails = [];

    private readonly List<IDisposable> _subscriptions = [];

    private int _userCount;

    public IReadOnlyDictionary<uint, IReadOnlyList<MoveStep>> Trails => _trails;

    public void Start()
    {
        _userCount++;

        if (_userCount > 1)
        {
            return;
        }

        _subscriptions.Add(connection.Subscribe<CharacterMovedEvent>(AddStep));
        _subscriptions.Add(connection.Subscribe<CharacterDisappearedEvent>(RemoveTrail));
    }

    public void Stop()
    {
        _userCount--;

        if (_userCount > 0)
        {
            return;
        }

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _trails.Clear();
    }

    public TimeSpan MeasureAge(MoveStep step)
    {
        return timeProvider.GetElapsedTime(step.Timestamp);
    }

    private void AddStep(CharacterMovedEvent moved)
    {
        var trail = _trails.GetValueOrDefault(moved.WorldId, []);

        _trails[moved.WorldId] =
        [
            .. trail.Where(step => MeasureAge(step) < TrailDuration),
            new MoveStep(moved.Points, timeProvider.GetTimestamp())
        ];
    }

    private void RemoveTrail(CharacterDisappearedEvent disappeared)
    {
        _trails.Remove(disappeared.WorldId);
    }
}
