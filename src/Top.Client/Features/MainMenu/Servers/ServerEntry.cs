using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace Top.Client.Features.MainMenu.Servers;

public class ServerEntry
{
    public const int DefaultPort = 1973;

    private const int LowestPort = 1;

    private const int HighestPort = 65535;

    private readonly List<AccountEntry> _accounts = [];

    public string Name { get; set; } = null!;

    public string Host { get; set; } = null!;

    public int Port { get; set; }

    public IReadOnlyList<AccountEntry> Accounts
    {
        get => _accounts;
        set => ReplaceAccounts(value);
    }

    [JsonIgnore]
    public AccountEntry? LastAccount => _accounts
        .Where(account => account.LastLogin.HasValue)
        .MaxBy(account => account.LastLogin);

    [JsonIgnore]
    public DateTime? LastLogin => LastAccount?.LastLogin;

    [JsonIgnore]
    public string Address => $"{Host}:{Port}";

    public override string ToString()
    {
        return $"{Name} - {Address}";
    }

    public static bool TryParse(string? name, string? host, string? port,
        [NotNullWhen(true)] out string? parsedName,
        [NotNullWhen(true)] out string? parsedHost,
        out int parsedPort)
    {
        parsedName = null;
        parsedHost = null;
        parsedPort = 0;

        string? trimmedName = name?.Trim();
        string? trimmedHost = host?.Trim();

        if (string.IsNullOrEmpty(trimmedName) || string.IsNullOrEmpty(trimmedHost))
        {
            return false;
        }

        if (!int.TryParse(port?.Trim(), out int number) || number < LowestPort || number > HighestPort)
        {
            return false;
        }

        parsedName = trimmedName;
        parsedHost = trimmedHost;
        parsedPort = number;

        return true;
    }

    public void RememberLogin(string accountName, DateTime time)
    {
        AccountEntry account = new AccountEntry(accountName, time.ToUniversalTime());

        int index = FindAccountIndex(accountName);

        if (index < 0)
        {
            _accounts.Add(account);
        }
        else
        {
            _accounts[index] = account;
        }
    }

    public void ForgetAccount(string accountName)
    {
        _accounts.RemoveAll(account => account.Name == accountName);
    }

    private int FindAccountIndex(string accountName)
    {
        return _accounts.FindIndex(account => account.Name == accountName);
    }

    private void ReplaceAccounts(IEnumerable<AccountEntry>? accounts)
    {
        _accounts.Clear();

        if (accounts == null)
        {
            return;
        }

        foreach (AccountEntry account in accounts)
        {
            if (account is not null && FindAccountIndex(account.Name) < 0)
            {
                _accounts.Add(account);
            }
        }
    }
}
