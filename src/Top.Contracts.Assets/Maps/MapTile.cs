namespace Top.Contracts.Assets.Maps
{
    public struct MapTile
    {
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
        public byte Cell00;
        public byte Cell10;
        public byte Cell01;
        public byte Cell11;
    }
}
