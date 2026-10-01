using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Top.Client.Core;
using Top.Logging;

namespace Top.Client.Features.MainMenu.Servers;

public class ServerStore
{
    private const string AccountNamesProperty = "AccountNames";

    private readonly string _path;

    private readonly List<ServerEntry> _entries = [];

    public ServerStore(string path)
    {
        _path = path;

        Read();
    }

    public IReadOnlyList<ServerEntry> Entries => _entries;

    public ServerEntry? LastServer => _entries
        .Where(entry => entry.LastLogin.HasValue)
        .MaxBy(entry => entry.LastLogin);

    public ServerEntry Add(string name, string host, int port)
    {
        ServerEntry entry = new ServerEntry { Name = name, Host = host, Port = port };

        _entries.Add(entry);

        Save();

        return entry;
    }

    public void Edit(ServerEntry entry, string name, string host, int port)
    {
        entry.Name = name;
        entry.Host = host;
        entry.Port = port;

        Save();
    }

    public void Remove(ServerEntry entry)
    {
        _entries.Remove(entry);

        Save();
    }

    public void RememberLogin(ServerEntry entry, string accountName, DateTime time)
    {
        entry.RememberLogin(accountName, time);

        Save();
    }

    public void ForgetAccount(ServerEntry entry, string accountName)
    {
        entry.ForgetAccount(accountName);

        Save();
    }

    private static ServerEntry? ToServerEntry(JsonElement row)
    {
        if (row.Deserialize<ServerEntry>() is not { } entry)
        {
            return null;
        }

        if (!row.TryGetProperty(nameof(ServerEntry.Accounts), out _)
            && row.TryGetProperty(AccountNamesProperty, out JsonElement names)
            && names.ValueKind == JsonValueKind.Array)
        {
            entry.Accounts = names.EnumerateArray()
                .Where(name => name.ValueKind == JsonValueKind.String)
                .Select(name => new AccountEntry(name.GetString()!, null))
                .ToList();
        }

        return entry;
    }

    private void Save()
    {
        try
        {
            JsonFile.Write(_path, _entries);
        }
        catch (IOException exception)
        {
            Log.Error($"{_path} did not save", exception);
        }
    }

    private void Read()
    {
        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            using JsonDocument saved = JsonDocument.Parse(File.ReadAllText(_path));

            if (saved.RootElement.ValueKind != JsonValueKind.Array)
            {
                Log.Error($"{_path} is not a list of servers, starting with no servers");

                return;
            }

            foreach (JsonElement row in saved.RootElement.EnumerateArray())
            {
                if (ToServerEntry(row) is { } entry)
                {
                    _entries.Add(entry);
                }
            }
        }
        catch (JsonException exception)
        {
            Log.Error($"{_path} did not parse, starting with no servers", exception);
        }
    }
}
