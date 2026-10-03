using System;
using System.Diagnostics;

namespace Top.Client.Cli.Features.Chat;

public class ChatState(TimeSpan sayInterval)
{
    public static readonly TimeSpan DefaultSayInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan NetworkMargin = TimeSpan.FromSeconds(0.1);

    private readonly Stopwatch _sinceLastSay = new();

    public TimeSpan TimeUntilNextSay
    {
        get
        {
            if (!_sinceLastSay.IsRunning)
            {
                return TimeSpan.Zero;
            }

            var left = sayInterval + NetworkMargin - _sinceLastSay.Elapsed;

            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    public void MarkSay()
    {
        _sinceLastSay.Restart();
    }
}
