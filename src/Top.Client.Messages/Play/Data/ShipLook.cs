namespace Top.Client.Messages.Play.Data
{
    public class ShipLook
    {
        public ShipLook(int shipId, int bowId, int engineId)
        {
            ShipId = shipId;
            BowId = bowId;
            EngineId = engineId;
        }

        public int ShipId { get; }

        public int BowId { get; }

        public int EngineId { get; }
    }
}
