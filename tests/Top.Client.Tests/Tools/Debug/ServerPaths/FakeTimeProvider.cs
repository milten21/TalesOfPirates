using System;

namespace Top.Client.Tests.Tools.Debug.ServerPaths;

public class FakeTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        return _timestamp;
    }

    public void Advance(TimeSpan time)
    {
        _timestamp += time.Ticks;
    }
}
