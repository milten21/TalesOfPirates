using Top.Legacy.MindPower.Textures;

namespace Top.Legacy.MindPower.Cursors
{
    public class CursorFrame(RgbaImage image, int hotspotX, int hotspotY)
    {
        public RgbaImage Image { get; } = image;

        public int HotspotX { get; } = hotspotX;

        public int HotspotY { get; } = hotspotY;
    }
}
