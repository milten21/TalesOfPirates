using System;
using Godot;
using Top.Logging;

namespace Top.Client.Core;

public class GodotLogWriter : ILogWriter
{
    public void Write(LogLevel level, string message, Exception? exception)
    {
        var text = exception == null ? message : $"{message}: {exception}";

        switch (level)
        {
            case LogLevel.Error:
                GD.PushError(text);
                break;
            case LogLevel.Warning:
                GD.PushWarning(text);
                break;
            default:
                GD.Print(text);
                break;
        }
    }
}
