using Godot;
using Top.Logging;

namespace Top.Client.UI;

public static class ThemeLibrary
{
    public static Theme? Load(string? name)
    {
        var path = ThemePaths.ToPath(name);

        if (ResourceLoader.Exists(path) && ResourceLoader.Load<Theme>(path) is { } theme)
        {
            return theme;
        }

        var fallback = ThemePaths.ToPath(null);

        if (path != fallback)
        {
            Log.Warning($"theme {name} did not load from {path}, falling back to {ThemePaths.DefaultName}");

            return Load(null);
        }

        Log.Error($"the default theme did not load from {fallback}");

        return null;
    }
}
