namespace Top.Contracts.Assets.Maps
{
    /// <summary>
    /// Represents a single layer of a map tile, defining visual properties and masking behavior.
    /// </summary>
    public struct MapTileLayer
    {
        public byte TerrainId;
        public byte MaskIndex;
    }
}
