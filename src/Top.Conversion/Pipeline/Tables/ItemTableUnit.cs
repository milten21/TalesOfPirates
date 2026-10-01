using System.Collections.Generic;
using System.Linq;
using Top.Contracts;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Items;
using Top.Legacy.Tables.Custom;
using Top.Legacy.Tables.Records;
using Top.Logging;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps every iteminfo row to an item entry: the name and the icon, the
    /// item type, which selects the stance of a held item, the equipment links
    /// an item can take and the model each body type shows it as.
    /// Worn items are skinned body parts on the links 0 to 4, or apparel on
    /// the links 19 to 23 shown over them; held items ride the rig dummy
    /// named by their hand link. Any other item has no model paths.
    /// Every item also carries what the inventory shows and checks: trade and
    /// drop rules, stack limit, price, requirements (the characters that can
    /// wear it among them, none when any can), stat bonuses, durability,
    /// energy, sockets, forge values, script effect, description and cooldown.
    /// Boat part types keep only their identity, so a boat part in a bag still
    /// shows its name and icon until a boat feature maps the rest.
    /// </summary>
    public class ItemTableUnit(ClientTables tables) : ITableUnit
    {
        private const int BodyLinkCount = 5;
        private const int ApparelLinkOffset = 19;
        private const int LeftHandLink = 6;
        private const int RightHandLink = 9;
        private const string NoScriptEffect = "0";
        private const int AnyBody = -1;

        public string Name => "items";

        public string Path => ItemTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Items?.Select(Entry);
        }

        private static ItemEntry Entry(ItemInfoRecord item)
        {
            var links = item.EquipSlots ?? [];
            var entry = new ItemEntry
            {
                Id = item.Id,
                Name = item.Name,
                IconPath = ItemIconWriter.ContentPath(item),
                Type = (int)item.Type,
                EquipLinks = links,
                ModelPaths = SelectModelPaths(item, links),
            };

            if (IsBoatPart(item.Type))
            {
                return entry;
            }

            entry.Tradeable = item.Tradeable;
            entry.Pickable = item.Pickable;
            entry.Discardable = item.Discardable;
            entry.Deletable = item.Deletable;
            entry.StackLimit = item.PileMax;
            entry.Price = item.Price;
            entry.RequiredLevel = item.NeedLevel;
            entry.Jobs = item.Jobs ?? [];
            entry.Bodies = item.Body is { Length: > 0 } bodies && bodies[0] != AnyBody ? bodies : [];
            entry.NeedLinks = item.NeedSlots ?? [];
            entry.Stats = SelectStats(item);
            entry.Durability = Range(item.Endure);
            entry.Energy = Range(item.Energy);
            entry.Sockets = item.Holes;
            entry.ForgeLevel = item.ForgeLevel;
            entry.ForgeCap = item.ForgeSteady;
            entry.ScriptEffect = string.IsNullOrEmpty(item.AttrEffect) || item.AttrEffect == NoScriptEffect
                ? null
                : item.AttrEffect;
            entry.Description = item.Description;
            entry.Cooldown = item.Cooldown;

            return entry;
        }

        private static bool IsBoatPart(ItemType type)
        {
            return type is ItemType.Boat or ItemType.Hull or ItemType.Embolon or ItemType.Mobility
                or ItemType.Artillery or ItemType.Airscrew or ItemType.BoatSign;
        }

        private static Dictionary<CharacterStat, ItemStatBonus> SelectStats(ItemInfoRecord item)
        {
            var bonuses = new (CharacterStat Stat, int Coefficient, int[] Values)[]
            {
                (CharacterStat.Strength, item.StrengthCoef, item.StrengthValue),
                (CharacterStat.Agility, item.AgilityCoef, item.AgilityValue),
                (CharacterStat.Accuracy, item.AccuracyCoef, item.AccuracyValue),
                (CharacterStat.Constitution, item.ConstitutionCoef, item.ConstitutionValue),
                (CharacterStat.Spirit, item.SpiritCoef, item.SpiritValue),
                (CharacterStat.Luck, item.LuckCoef, item.LuckValue),
                (CharacterStat.AttackSpeed, item.AttackSpeedCoef, item.AttackSpeedValue),
                (CharacterStat.AttackRange, item.AttackDistanceCoef, item.AttackRangeValue),
                (CharacterStat.MinAttack, item.MinAttackCoef, item.MinAttackValue),
                (CharacterStat.MaxAttack, item.MaxAttackCoef, item.MaxAttackValue),
                (CharacterStat.Defense, item.DefenseCoef, item.DefenseValue),
                (CharacterStat.MaxHealth, item.MaxHpCoef, item.MaxHpValue),
                (CharacterStat.MaxMana, item.MaxSpCoef, item.MaxSpValue),
                (CharacterStat.Dodge, item.DodgeCoef, item.DodgeValue),
                (CharacterStat.Hit, item.HitCoef, item.HitValue),
                (CharacterStat.Critical, item.CriticalCoef, item.CriticalValue),
                (CharacterStat.MagicFind, item.MagicFindCoef, item.MagicFindValue),
                (CharacterStat.HealthRecovery, item.HpRecoveryCoef, item.HpRecoveryValue),
                (CharacterStat.ManaRecovery, item.SpRecoveryCoef, item.SpRecoveryValue),
                (CharacterStat.MoveSpeed, item.MoveSpeedCoef, item.MoveSpeedValue),
                (CharacterStat.GatheringSpeed, item.CollectCoef, item.CollectValue),
                (CharacterStat.PhysicalResist, 0, item.PhysicalResist),
            };
            var stats = new Dictionary<CharacterStat, ItemStatBonus>();

            foreach (var (stat, coefficient, values) in bonuses)
            {
                var range = Range(values);

                if (coefficient != 0 || range.Minimum != 0 || range.Maximum != 0)
                {
                    stats[stat] = new ItemStatBonus
                    {
                        Coefficient = coefficient, Minimum = range.Minimum, Maximum = range.Maximum,
                    };
                }
            }

            return stats;
        }

        private static ItemRange Range(int[] values)
        {
            return new ItemRange
            {
                Minimum = values is { Length: > 0 } ? values[0] : 0,
                Maximum = values is { Length: > 1 } ? values[1] : 0,
            };
        }

        private static string[] SelectModelPaths(ItemInfoRecord item, int[] links)
        {
            var worn = ItemModules.IsWearable(item.Type);

            if (!worn && !links.Any(IsHandLink))
            {
                return [];
            }

            var modelPaths = new string[ItemConverter.Models];

            for (var bodyType = 0; bodyType < modelPaths.Length; bodyType++)
            {
                if (ItemModules.TryGetModule(item, bodyType, out var module))
                {
                    modelPaths[bodyType] = OutputPaths.ModelContentPath(worn ? ContentKind.Character : ContentKind.Item,
                        module);
                }
            }

            if (modelPaths.All(modelPath => modelPath == null))
            {
                return [];
            }

            if (worn && !links.Any(IsBodyLink))
            {
                Log.Warning($"item {item.Id} '{item.Name}': a worn item with no body link, the item has no model");

                return [];
            }

            return modelPaths;
        }

        private static bool IsBodyLink(int link)
        {
            return (link >= 0 && link < BodyLinkCount)
                   || (link >= ApparelLinkOffset && link < ApparelLinkOffset + BodyLinkCount);
        }

        private static bool IsHandLink(int link)
        {
            return link is LeftHandLink or RightHandLink;
        }
    }
}
