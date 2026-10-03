using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Top.Client.Cli.Core;
using Top.Client.Cli.Features.Chat;

namespace Top.Client.Cli;

internal class Arguments
{
    private const int MaximumSayIntervalSeconds = 60;

    public const string Usage = """
        usage: Top.Client.Cli --server <host>:<port> --account <name> --password <text>
                              (--scenario <file> | --live [--port <port>]) [--say-interval <seconds>]

          --server <host>:<port>      gate server address
          --account <name>            account name
          --password <text>           account password
          --scenario <file>           file mode: run the scenario file, with one step on each line. Empty lines
                                      and lines that start with # are skipped
          --live                      live mode: take one step at a time over HTTP on localhost
          --port <port>               the live mode port, 7973 when not given
          --say-interval <seconds>    the server's minimum time between two chat lines, 1 when not given
          --help, -h                  this text, as the only argument

        File mode stops at the first failed step and exits with code 1.
        Live mode: POST one step line to http://localhost:<port>/step, and the response gives its result.
        GET http://localhost:<port>/events gives the events that arrived since the last read. quit ends the run.
        A bad argument exits with code 2. A port that cannot be used exits with code 3.

        """;

    private Arguments(ServerAccount account, string? scenarioPath, int livePort, TimeSpan sayInterval)
    {
        Account = account;
        ScenarioPath = scenarioPath;
        LivePort = livePort;
        SayInterval = sayInterval;
    }

    public ServerAccount Account { get; }

    public string? ScenarioPath { get; }

    [MemberNotNullWhen(false, nameof(ScenarioPath))]
    public bool IsLive => ScenarioPath == null;

    public int LivePort { get; }

    public TimeSpan SayInterval { get; }

    public static bool TryParse(IReadOnlyList<string> values, [NotNullWhen(true)] out Arguments? arguments,
        [NotNullWhen(false)] out string? error)
    {
        string? server = null;
        string? account = null;
        string? password = null;
        string? scenarioPath = null;
        string? livePort = null;
        string? sayInterval = null;
        var isLive = false;

        arguments = null;

        for (var index = 0; index < values.Count; index++)
        {
            var name = values[index];

            switch (name)
            {
                case "--server":
                    if (!TryReadValue(values, name, ref index, out server, out error))
                    {
                        return false;
                    }

                    break;

                case "--account":
                    if (!TryReadValue(values, name, ref index, out account, out error))
                    {
                        return false;
                    }

                    break;

                case "--password":
                    if (!TryReadValue(values, name, ref index, out password, out error))
                    {
                        return false;
                    }

                    break;

                case "--scenario":
                    if (!TryReadValue(values, name, ref index, out scenarioPath, out error))
                    {
                        return false;
                    }

                    break;

                case "--live":
                    isLive = true;

                    break;

                case "--port":
                    if (!TryReadValue(values, name, ref index, out livePort, out error))
                    {
                        return false;
                    }

                    break;

                case "--say-interval":
                    if (!TryReadValue(values, name, ref index, out sayInterval, out error))
                    {
                        return false;
                    }

                    break;

                default:
                    error = $"unknown argument '{name}'";

                    return false;
            }
        }

        if (server == null || account == null || password == null)
        {
            error = "--server, --account and --password are all needed";

            return false;
        }

        if (isLive == (scenarioPath != null))
        {
            error = "give --scenario <file> or --live, and not both";

            return false;
        }

        if (livePort != null && !isLive)
        {
            error = "--port is for --live only";

            return false;
        }

        var separator = server.LastIndexOf(':');
        var host = separator > 0 ? server[..separator] : string.Empty;

        if (host.Length == 0 || !TryParsePort(server[(separator + 1)..], out var port))
        {
            error = $"--server needs <host>:<port>, got '{server}'";

            return false;
        }

        var parsedLivePort = LiveListener.DefaultPort;

        if (livePort != null && !TryParsePort(livePort, out parsedLivePort))
        {
            error = $"--port needs a port from 1 to {ushort.MaxValue}, got '{livePort}'";

            return false;
        }

        var parsedSayInterval = ChatState.DefaultSayInterval;

        if (sayInterval != null && !TryParseSayInterval(sayInterval, out parsedSayInterval, out error))
        {
            return false;
        }

        arguments = new Arguments(new ServerAccount(host, port, account, password), scenarioPath, parsedLivePort,
            parsedSayInterval);
        error = null;

        return true;
    }

    private static bool TryReadValue(IReadOnlyList<string> values, string name, ref int index,
        [NotNullWhen(true)] out string? value, [NotNullWhen(false)] out string? error)
    {
        if (index + 1 >= values.Count)
        {
            value = null;
            error = $"{name} needs a value";

            return false;
        }

        value = values[++index];
        error = null;

        return true;
    }

    private static bool TryParsePort(string text, out int port)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out port) &&
               port is > 0 and <= ushort.MaxValue;
    }

    private static bool TryParseSayInterval(string text, out TimeSpan sayInterval,
        [NotNullWhen(false)] out string? error)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds is >= 0 and <= MaximumSayIntervalSeconds)
        {
            sayInterval = TimeSpan.FromSeconds(seconds);
            error = null;

            return true;
        }

        sayInterval = TimeSpan.Zero;
        error = $"--say-interval needs a number of seconds from 0 to {MaximumSayIntervalSeconds}, got '{text}'";

        return false;
    }
}
