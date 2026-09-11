namespace Top.Contracts.Assets.Maps
{
    public static class MapTexturePaths
    {
        public const int WaterFrameCount = 30;

        public const string MaskAtlas = "textures/terrain/alpha/total.png";

        public static string GetWaterFramePath(int frame) => $"textures/terrain/water/ocean_h.{frame + 1:00}.png";
    }
}
