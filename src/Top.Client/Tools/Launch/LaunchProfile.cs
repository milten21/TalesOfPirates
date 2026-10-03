using System.IO;
using System.Text.Json;
using Top.Client.Core;
using Top.Logging;

namespace Top.Client.Tools.Launch;

public class LaunchProfile
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public required string Account { get; init; }

    public required string Password { get; init; }

    public required string CharacterName { get; init; }

    public static LaunchProfile? Read(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonFile.Read<LaunchProfile>(path);
        }
        catch (JsonException exception)
        {
            Log.Error($"{path} did not parse as a launch profile", exception);

            return null;
        }
    }
}
