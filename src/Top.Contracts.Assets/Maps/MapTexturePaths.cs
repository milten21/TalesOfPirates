namespace Top.Contracts.Assets.Maps
{
    public static class MapTexturePaths
    {
        public const int WaterFrames = 30;

        public const string Masks = "textures/terrain/alpha/total.png";

        public static string Water(int frame) => $"textures/terrain/water/ocean_h.{frame + 1:00}.png";
    }
}
