using Godot;

namespace Top.Client.Features.Hud;

public partial class HudPlate : Control
{
    private static readonly StringName GradientIcon = "gradient";

    private static readonly StringName BottomPercentConstant = "bottom_percent";

    private static readonly StringName EdgeWidthConstant = "edge_width";

    private static readonly StringName EdgeColor = "edge";

    private static readonly Color[] UntintedColors = [Colors.White];

    public override void _Draw()
    {
        var size = Size;

        if (size.X <= 0f || size.Y <= 0f)
        {
            return;
        }

        var bottom = size.X * GetThemeConstant(BottomPercentConstant) / 100f;
        Vector2[] points = [Vector2.Zero, new Vector2(size.X, 0f), new Vector2(bottom, size.Y), new Vector2(0f, size.Y)];
        var uvs = new Vector2[points.Length];

        for (var index = 0; index < points.Length; index++)
        {
            uvs[index] = new Vector2(points[index].X / size.X, 0.5f);
        }

        DrawPolygon(points, UntintedColors, uvs, GetThemeIcon(GradientIcon));

        var edgeWidth = GetThemeConstant(EdgeWidthConstant);

        if (edgeWidth > 0)
        {
            DrawRect(new Rect2(0f, 0f, edgeWidth, size.Y), GetThemeColor(EdgeColor));
        }
    }
}
