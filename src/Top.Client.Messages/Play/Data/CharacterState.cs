using System;
using System.Numerics;

namespace Top.Client.Messages.Play.Data
{
    public record CharacterState
    {
        public CharacterState(uint worldId, CharacterControlType controlType, int typeId, string name,
            Vector2 position, int angle, int existState, uint teamLeaderId, int guildId, string guildName,
            string guildMotto,
            int guildPermission, int level, int health, int maxHealth, int mana, int maxMana, int job,
            long experience, long levelThreshold, long nextLevelThreshold, long gold, float moveSpeed,
            CharacterLook? look, ShipLook? shipLook)
        {
            WorldId = worldId;
            ControlType = controlType;
            TypeId = typeId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Position = position;
            Angle = angle;
            ExistState = existState;
            TeamLeaderId = teamLeaderId;
            GuildId = guildId;
            GuildName = guildName ?? throw new ArgumentNullException(nameof(guildName));
            GuildMotto = guildMotto ?? throw new ArgumentNullException(nameof(guildMotto));
            GuildPermission = guildPermission;
            Level = level;
            Health = health;
            MaxHealth = maxHealth;
            Mana = mana;
            MaxMana = maxMana;
            Job = job;
            Experience = experience;
            LevelThreshold = levelThreshold;
            NextLevelThreshold = nextLevelThreshold;
            Gold = gold;
            MoveSpeed = moveSpeed;
            Look = look;
            ShipLook = shipLook;
        }

        public uint WorldId { get; init; }

        public CharacterControlType ControlType { get; init; }

        public int TypeId { get; init; }

        public string Name { get; init; }

        public Vector2 Position { get; init; }

        public int Angle { get; init; }

        public int ExistState { get; init; }

        public uint TeamLeaderId { get; init; }

        public int GuildId { get; init; }

        public string GuildName { get; init; }

        public string GuildMotto { get; init; }

        public int GuildPermission { get; init; }

        public int Level { get; init; }

        public int Health { get; init; }

        public int MaxHealth { get; init; }

        public int Mana { get; init; }

        public int MaxMana { get; init; }

        public int Job { get; init; }

        public long Experience { get; init; }

        public long LevelThreshold { get; init; }

        public long NextLevelThreshold { get; init; }

        public long Gold { get; init; }

        public float MoveSpeed { get; init; }

        public CharacterLook? Look { get; init; }

        public ShipLook? ShipLook { get; init; }
    }
}
