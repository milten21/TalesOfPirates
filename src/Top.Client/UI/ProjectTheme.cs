using Godot;
using Top.Logging;

namespace Top.Client.UI;

public static class ProjectTheme
{
    public static void Apply(Theme? selected)
    {
        if (selected == null)
        {
            return;
        }

        if (ThemeDB.GetProjectTheme() is not { } project)
        {
            Log.Error("the project has no project theme to apply the selected theme to");

            return;
        }

        if (project == selected)
        {
            return;
        }

        project.Clear();
        project.MergeWith(selected);
        project.DefaultFont = selected.DefaultFont;
        project.DefaultFontSize = selected.DefaultFontSize;
        project.DefaultBaseScale = selected.DefaultBaseScale;
    }
}
