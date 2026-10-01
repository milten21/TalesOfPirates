using Godot;

namespace Top.Client.UI;

public partial class HoledDim : Control
{
    private static readonly StringName PanelStyle = "panel";

    private Vector2 _holeCentre;

    [Export] public Texture2D HoleTexture { get; set; } = null!;

    [Export] public Vector2 HoleSize { get; set; } = new Vector2(230f, 460f);

    public void PlaceHole(Vector2 centre)
    {
        if (centre == _holeCentre)
        {
            return;
        }

        _holeCentre = centre;

        QueueRedraw();
    }

    public override void _Draw()
    {
        var style = GetThemeStylebox(PanelStyle);
        var hole = new Rect2(_holeCentre - (HoleSize * 0.5f), HoleSize);
        var size = Size;

        DrawTextureRect(HoleTexture, hole, tile: false);

        DrawDim(style, new Rect2(0f, 0f, size.X, hole.Position.Y));
        DrawDim(style, new Rect2(0f, hole.End.Y, size.X, size.Y - hole.End.Y));
        DrawDim(style, new Rect2(0f, hole.Position.Y, hole.Position.X, hole.Size.Y));
        DrawDim(style, new Rect2(hole.End.X, hole.Position.Y, size.X - hole.End.X, hole.Size.Y));
    }

    private void DrawDim(StyleBox style, Rect2 area)
    {
        if (area.Size.X <= 0f || area.Size.Y <= 0f)
        {
            return;
        }

        DrawStyleBox(style, area);
    }
}
