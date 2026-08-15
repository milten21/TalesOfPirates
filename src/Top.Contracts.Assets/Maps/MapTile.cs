namespace Top.Contracts.Assets.Maps
{
    /// <summary>
    /// Represents a single tile in a map.
    /// </summary>
    public struct MapTile
    {
        /// <summary>
        /// The terrain the original's stand-in tile carried over ground no
        /// section wrote (UNDERWATER_TEXNO, MPMap.h).
        /// </summary>
        public const byte UnderwaterTerrain = 22;

        public static readonly MapTile Underwater = new MapTile
        {
            Height = -2f,
            ColorR = byte.MaxValue,
            ColorG = byte.MaxValue,
            ColorB = byte.MaxValue,
            Layer0 = new MapTileLayer
            {
                TerrainId = UnderwaterTerrain,
                MaskIndex = 15,
            }
        };

        public float Height;
        public byte ColorR;
        public byte ColorG;
        public byte ColorB;
        public MapTileLayer Layer0;
        public MapTileLayer Layer1;
        public MapTileLayer Layer2;
        public MapTileLayer Layer3;
        public ushort Region;
        public byte Island;
        public byte Corner00;
        public byte Corner10;
        public byte Corner01;
        public byte Corner11;
    }
}
