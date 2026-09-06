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
            ushort clientVersion = 32125,
            bool isEncrypted = true)
        {
            FrameFormat = frameFormat ?? new FrameFormat();
            IdleInterval = idleInterval ?? TimeSpan.FromSeconds(25);
            ReadTimeout = readTimeout ?? TimeSpan.FromSeconds(30);
            ClientVersion = clientVersion;
            IsEncrypted = isEncrypted;

            RequireInterval(IdleInterval, nameof(idleInterval));
            RequireInterval(ReadTimeout, nameof(readTimeout));
        }

        public FrameFormat FrameFormat { get; }

        public TimeSpan IdleInterval { get; }

        public TimeSpan ReadTimeout { get; }

        public ushort ClientVersion { get; }

        public bool IsEncrypted { get; }

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
