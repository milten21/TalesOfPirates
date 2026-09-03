using UnityEngine;

namespace Top.Client.Game.World.Terrain
{
    /// <summary>
    /// Terrain texture coordinates as the original laid them out, with v
    /// flipped for Unity's bottom-up images.
    /// </summary>
    public static class TerrainUv
    {
        private const int TilesPerTexture = 4;
        private const int TerrainMasksColumns = 4;

        private const float CellSize = 1f / TerrainMasksColumns;
        private const float CellInset = 0.01f;

        public static Vector2 BaseUv(int tileX, int tileY, int cornerX, int cornerY)
        {
            var u = ((tileX % TilesPerTexture) + cornerX) / (float)TilesPerTexture;
            var v = ((tileY % TilesPerTexture) + cornerY) / (float)TilesPerTexture;

            return new Vector2(u, 1f - v);
        }

        public static Vector2 BaseUvSpan(int tiles, int cornerX, int cornerY)
        {
            var span = cornerX * tiles / (float)TilesPerTexture;

            return new Vector2(span, 1f - (cornerY * tiles / (float)TilesPerTexture));
        }

        public static Vector2 MaskUv(int index, int cornerX, int cornerY)
        {
            var cell = Mathf.Max(index - 1, 0);
            var span = CellSize - (2f * CellInset);
            var u = ((cell % TerrainMasksColumns) * CellSize) + CellInset + (cornerX * span);
            var v = ((cell / TerrainMasksColumns) * CellSize) + CellInset + (cornerY * span);

            return new Vector2(u, 1f - v);
        }
    }
}
