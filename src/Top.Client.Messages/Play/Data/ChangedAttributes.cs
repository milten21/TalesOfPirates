namespace Top.Client.Messages.Play.Data
{
    public record ChangedAttributes
    {
        public int? Level { get; init; }

        public int? Health { get; init; }

        public int? MaxHealth { get; init; }

        public int? Mana { get; init; }

        public int? MaxMana { get; init; }

        public int? Job { get; init; }

        public long? Experience { get; init; }

        public long? LevelThreshold { get; init; }

        public long? NextLevelThreshold { get; init; }

        public long? Gold { get; init; }

        public float? MoveSpeed { get; init; }
    }
}
