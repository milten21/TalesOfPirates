namespace Top.Contracts.Assets.Maps
{
    /// <summary>
    /// Represents the placement of an object within a map.
    /// </summary>
    public struct MapPlacement
    {
        public PlacementKind Kind;
        public int Id;
        public float X;
        public float Y;
        public float HeightOffset;
        public float Yaw;
    }
}
