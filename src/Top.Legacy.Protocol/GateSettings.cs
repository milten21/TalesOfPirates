using System;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol
{
    public class GateSettings
    {
        public GateSettings(
            FrameFormat frameFormat = null,
            TimeSpan? idleInterval = null,
            TimeSpan? readTimeout = null,
            TimeSpan? connectTimeout = null,
            ushort clientVersion = 32125)
        {
            FrameFormat = frameFormat ?? new FrameFormat();
            IdleInterval = idleInterval ?? TimeSpan.FromSeconds(2);
            ReadTimeout = readTimeout ?? TimeSpan.FromSeconds(30);
            ConnectTimeout = connectTimeout ?? TimeSpan.FromSeconds(40);
            ClientVersion = clientVersion;

            RequireInterval(IdleInterval, nameof(idleInterval));
            RequireInterval(ReadTimeout, nameof(readTimeout));
            RequireInterval(ConnectTimeout, nameof(connectTimeout));
        }

        public FrameFormat FrameFormat { get; }

        public TimeSpan IdleInterval { get; }

        public TimeSpan ReadTimeout { get; }

        public TimeSpan ConnectTimeout { get; }

        public ushort ClientVersion { get; }

        private static void RequireInterval(TimeSpan interval, string parameterName)
        {
            if (interval <= TimeSpan.Zero || interval.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName, interval, "An interval is above zero and fits a count of milliseconds.");
            }
        }
    }
}
