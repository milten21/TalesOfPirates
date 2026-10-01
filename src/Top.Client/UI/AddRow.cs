using Godot;

namespace Top.Client.UI;

public partial class AddRow : CaptionButton
{
    private const float BorderWidth = 1f;

    private const int CornerPointCount = 8;

    [Export] public Label IconLabel { get; set; } = null!;

    public override void _Draw()
    {
        base._Draw();

        DrawDashedBorder();
    }

    protected override void ApplyFontColor(Color color)
    {
        base.ApplyFontColor(color);

        IconLabel.AddThemeColorOverride("font_color", color);
    }

    private void DrawDashedBorder()
    {
        var color = GetThemeColor("dash_color");
        float dashLength = GetThemeConstant("dash_length");
        float radius = GetThemeConstant("dash_corner_radius");

        float inset = BorderWidth / 2f;
        float left = inset;
        float top = inset;
        float right = Size.X - inset;
        float bottom = Size.Y - inset;

        DrawDashedLine(new Vector2(left + radius, top), new Vector2(right - radius, top), color, BorderWidth,
            dashLength);
        DrawDashedLine(new Vector2(right, top + radius), new Vector2(right, bottom - radius), color, BorderWidth,
            dashLength);
        DrawDashedLine(new Vector2(right - radius, bottom), new Vector2(left + radius, bottom), color, BorderWidth,
            dashLength);
        DrawDashedLine(new Vector2(left, bottom - radius), new Vector2(left, top + radius), color, BorderWidth,
            dashLength);

        float arcRadius = radius - inset;

        DrawCorner(new Vector2(radius, radius), arcRadius, Mathf.Pi, color);
        DrawCorner(new Vector2(Size.X - radius, radius), arcRadius, -Mathf.Pi / 2f, color);
        DrawCorner(new Vector2(Size.X - radius, Size.Y - radius), arcRadius, 0f, color);
        DrawCorner(new Vector2(radius, Size.Y - radius), arcRadius, Mathf.Pi / 2f, color);
    }

    private void DrawCorner(Vector2 center, float radius, float startAngle, Color color)
    {
        DrawArc(center, radius, startAngle, startAngle + Mathf.Pi / 2f, CornerPointCount, color, BorderWidth,
            antialiased: true);
    }
}
