using System;
using System.Collections.Generic;
using Top.Logging;

namespace Top.Client.LegacyBridge.Tests
{
    public class LogCapture : ILogWriter
    {
        private readonly object _lock = new();

        public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = [];

        public void Write(LogLevel level, string message, Exception exception)
        {
            lock (_lock)
            {
                Entries.Add((level, message, exception));
            }
        }
    }
}
