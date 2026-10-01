namespace Top.Client.Messages.Play.Events
{
    public class FairyConditionChangedEvent : IServerEvent
    {
        public FairyConditionChangedEvent(uint worldId, int itemId, int stamina, int growth)
        {
            WorldId = worldId;
            ItemId = itemId;
            Stamina = stamina;
            Growth = growth;
        }

        public uint WorldId { get; }

        public int ItemId { get; }

        public int Stamina { get; }

        public int Growth { get; }
    }
}
