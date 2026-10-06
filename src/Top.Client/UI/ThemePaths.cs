namespace Top.Client.UI;

public static class ThemePaths
{
    public const string DefaultName = "Default";

    public const string BaseFolder = Folder + DefaultName + "/";

    private const string Folder = "res://UI/Assets/";

    private const string Extension = ".tres";

    private static readonly char[] Separators = ['/', '\\', ':'];

    public static string ToPath(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name.IndexOfAny(Separators) >= 0
            || name.Contains(".."))
        {
            return Folder + DefaultName + Extension;
        }

        return Folder + name.Trim() + Extension;
    }
}
