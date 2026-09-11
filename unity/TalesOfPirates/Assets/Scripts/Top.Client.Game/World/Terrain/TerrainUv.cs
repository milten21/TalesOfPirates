using UnityEngine;

namespace Top.Client.Game.World.Terrain
{
    public static class TerrainUv
    {
        private const int TilesPerTexture = 4;
        private const int MaskAtlasColumns = 4;

        private const float MaskSize = 1f / MaskAtlasColumns;
        private const float MaskInset = 0.01f;

        public static Vector2 BaseUvAt(int tileX, int tileY, int cornerX, int cornerY)
        {
            var u = ((tileX % TilesPerTexture) + cornerX) / (float)TilesPerTexture;
            var v = ((tileY % TilesPerTexture) + cornerY) / (float)TilesPerTexture;

            return new Vector2(u, 1f - v);
        }

        public static Vector2 BaseUvAtSpanCorner(int tiles, int cornerX, int cornerY)
        {
            var span = cornerX * tiles / (float)TilesPerTexture;

            return new Vector2(span, 1f - (cornerY * tiles / (float)TilesPerTexture));
        }

        public static Vector2 MaskUvAt(int index, int cornerX, int cornerY)
        {
            var mask = Mathf.Max(index - 1, 0);
            var span = MaskSize - (2f * MaskInset);
            var u = ((mask % MaskAtlasColumns) * MaskSize) + MaskInset + (cornerX * span);
            var v = ((mask / MaskAtlasColumns) * MaskSize) + MaskInset + (cornerY * span);

            return new Vector2(u, 1f - v);
        }
    }
}
