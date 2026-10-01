using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages.Inventory.Data;
using Top.Contracts;
using Top.Legacy.Protocol.Messages.Characters;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class BagSyncMapper
    {
        private static readonly CharacterStat[] StatsByAttribute =
        {
            CharacterStat.Strength,
            CharacterStat.Agility,
            CharacterStat.Accuracy,
            CharacterStat.Constitution,
            CharacterStat.Spirit,
            CharacterStat.Luck,
            CharacterStat.AttackSpeed,
            CharacterStat.AttackRange,
            CharacterStat.MinAttack,
            CharacterStat.MaxAttack,
            CharacterStat.Defense,
            CharacterStat.MaxHealth,
            CharacterStat.MaxMana,
            CharacterStat.Dodge,
            CharacterStat.Hit,
            CharacterStat.Critical,
            CharacterStat.MagicFind,
            CharacterStat.HealthRecovery,
            CharacterStat.ManaRecovery,
            CharacterStat.MoveSpeed,
            CharacterStat.GatheringSpeed,
            CharacterStat.PhysicalResist,
        };

        public static BagSync ToBagSync(KitbagSync kitbag)
        {
            if (kitbag == null)
            {
                throw new ArgumentNullException(nameof(kitbag));
            }

            return new BagSync(
                kitbag.IsInit ? kitbag.Capacity : (int?)null,
                kitbag.Slots.Select(slot => new BagSlot(slot.GridId, ToBagItem(slot.Content))).ToArray());
        }

        internal static BagItem? ToBagItem(ItemGrid grid)
        {
            if (grid.IsEmpty)
            {
                return null;
            }

            return new BagItem(
                ItemId: grid.Id,
                NeedLevel: grid.NeedLevel,
                Count: grid.Count,
                Durability: grid.Endure[0],
                MaxDurability: grid.Endure[1],
                Energy: grid.Energy[0],
                MaxEnergy: grid.Energy[1],
                ForgeLevel: grid.ForgeLevel,
                IsValid: grid.IsValid,
                IsTradable: grid.IsTradable,
                IsLocked: grid.DbId != 0,
                Expiration: grid.Expiration,
                ForgeParameter: grid.Forge ?? 0,
                InstanceParameter: grid.InstanceId ?? 0,
                InstanceStats: ToInstanceStats(grid.InstanceAttributes));
        }

        private static IReadOnlyDictionary<CharacterStat, int> ToInstanceStats(
            IReadOnlyList<ItemInstanceAttribute>? attributes)
        {
            var stats = new Dictionary<CharacterStat, int>();

            foreach (var attribute in attributes ?? Array.Empty<ItemInstanceAttribute>())
            {
                var index = attribute.Id - ItemInstanceAttribute.Strength;

                if (index >= 0 && index < StatsByAttribute.Length)
                {
                    stats[StatsByAttribute[index]] = attribute.Value;
                }
            }

            return stats;
        }
    }
}
