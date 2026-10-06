using GdUnit4;
using Godot;
using Top.Client.UI;
using static GdUnit4.Assertions;

namespace Top.Client.EngineTests.Test.UI;

[TestSuite]
[RequireGodotRuntime]
public class ProjectThemeTests
{
    private const string ThemedType = "AddRow";

    private const string ThemedColorName = "dash_color";

    private static readonly Color ThemedColor = new Color(1, 1, 1, 0.4f);

    [TestCase]
    public void Control_under_canvas_layer_resolves_applied_theme()
    {
        var selected = ThemeLibrary.Load(ThemePaths.DefaultName)!;

        ProjectTheme.Apply(selected);

        var layer = AutoFree(new CanvasLayer())!;
        var control = new Control();

        layer.AddChild(control);

        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(layer);

        AssertThat(control.GetThemeColor(ThemedColorName, ThemedType)).IsEqual(ThemedColor);
        AssertThat(control.GetThemeDefaultFont()).IsEqual(selected.DefaultFont);
        AssertThat(control.GetThemeDefaultFontSize()).IsEqual(selected.DefaultFontSize);
    }
}
