using System.IO;
using System.Text.Json;

namespace Top.Client.Core;

public static class JsonFile
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static T? Read<T>(string path)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options);
    }

    public static void Write<T>(string path, T value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, Options));
    }
}
