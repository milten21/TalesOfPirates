using System;
using Top.Logging;

namespace Top.Client.Cli;

internal class ConsoleLog : ILogWriter
{
    public void Write(LogLevel level, string message, Exception? exception)
    {
        if (level < LogLevel.Warning)
        {
            return;
        }

        var label = level == LogLevel.Error ? "error" : "warn";

        Console.Error.WriteLine(exception == null
            ? $"{label}: {message}"
            : $"{label}: {message}: {exception.Message}");
    }
}
