using Godot;

namespace Top.Client.Features.Hud;

public partial class LevelHexagon : Control
{
    private const float CornerFraction = 0.2f;

    private static readonly StringName BorderColor = "border";

    private static readonly StringName FillColor = "fill";

    private static readonly StringName BorderWidthConstant = "border_width";

    public override void _Draw()
    {
        var outer = new Rect2(Vector2.Zero, Size);
        var inner = outer.Grow(-GetThemeConstant(BorderWidthConstant));

        if (inner.Size.X <= 0f || inner.Size.Y <= 0f)
        {
            return;
        }

        DrawColoredPolygon(BuildHexagon(outer), GetThemeColor(BorderColor));
        DrawColoredPolygon(BuildHexagon(inner), GetThemeColor(FillColor));
    }

    private static Vector2[] BuildHexagon(Rect2 area)
    {
        var corner = area.Size.X * CornerFraction;
        var middle = area.Position.Y + (area.Size.Y / 2f);

        return
        [
            new Vector2(area.Position.X + corner, area.Position.Y),
            new Vector2(area.End.X - corner, area.Position.Y),
            new Vector2(area.End.X, middle),
            new Vector2(area.End.X - corner, area.End.Y),
            new Vector2(area.Position.X + corner, area.End.Y),
            new Vector2(area.Position.X, middle)
        ];
    }
}
