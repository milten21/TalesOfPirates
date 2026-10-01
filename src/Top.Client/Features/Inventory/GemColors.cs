using Godot;

namespace Top.Client.Features.Inventory;

public static class GemColors
{
    public static Color ToColor(int rgb)
    {
        return Color.Color8((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }
}
