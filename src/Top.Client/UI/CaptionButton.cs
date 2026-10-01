using Godot;

namespace Top.Client.UI;

public partial class CaptionButton : Button
{
    [Export] public Label Caption { get; set; } = null!;

    public override void _Notification(int what)
    {
        if (what == NotificationThemeChanged && Caption is not null)
        {
            Caption.AddThemeFontOverride("font", GetThemeFont("font"));
            Caption.AddThemeFontSizeOverride("font_size", GetThemeFontSize("font_size"));
        }
    }

    public override void _Draw()
    {
        ApplyFontColor(GetThemeColor(FindFontColorName()));
    }

    protected virtual void ApplyFontColor(Color color)
    {
        Caption.AddThemeColorOverride("font_color", color);
    }

    private string FindFontColorName()
    {
        return GetDrawMode() switch
        {
            DrawMode.Disabled => "font_disabled_color",
            DrawMode.Hover => "font_hover_color",
            DrawMode.Pressed => "font_pressed_color",
            DrawMode.HoverPressed => "font_hover_pressed_color",
            _ => HasFocus() ? "font_focus_color" : "font_color"
        };
    }
}
