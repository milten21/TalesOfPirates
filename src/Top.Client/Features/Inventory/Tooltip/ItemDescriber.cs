using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;
using Top.Contracts;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;

namespace Top.Client.Features.Inventory.Tooltip;

public class ItemDescriber(
    ItemTypeTable itemTypes,
    ItemPrefixTable prefixes,
    GemTable gems,
    JobTable jobs)
{
    private const int DurabilityScale = 50;

    private const int ApparelDurability = 25_000;

    private const double WornDurability = 0.3;

    private const int HitGearType = 23;

    private const int DodgeGearType = 24;

    private const int LastRankedType = 27;

    private const int QuestItemType = 42;

    private const int WingType = 44;

    private const int GemType = 49;

    private const int RefiningGemType = 50;

    private const int NoBonusType = 75;

    private const int GemLadderReach = 2;

    private const int SocketDivisor = 1_000_000_000;

    private const int GemSlotCount = 3;

    private const int GemSlotBase = 1_000;

    private const int AnyJob = -1;

    private const long SecondsPerDay = 86_400;

    private static readonly int[] WeaponTypes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static readonly int[] DefenceTypes = [11, 22, 27];

    private static readonly int[] JewelleryTypes = [25, 26, 81, 82, 83];

    private static readonly int[] ArmourTypes = [20, 23, 24, 84, 88];

    private static readonly int[] FairyTypes = [59, 67];

    private static readonly int[] ToolTypes = [18, 19, 29];

    private static readonly int[] UnrankedGearTypes = [18, 19, 21];

    private static readonly string[] BodyNames = ["Lance", "Carsise", "Phyllis", "Ami"];

    private static readonly CharacterStat[] StatOrder =
    [
        CharacterStat.Strength, CharacterStat.Agility, CharacterStat.Accuracy, CharacterStat.Constitution,
        CharacterStat.Spirit, CharacterStat.Luck, CharacterStat.AttackSpeed, CharacterStat.AttackRange,
        CharacterStat.MinAttack, CharacterStat.MaxAttack, CharacterStat.Defense, CharacterStat.MaxHealth,
        CharacterStat.MaxMana, CharacterStat.Dodge, CharacterStat.Hit, CharacterStat.Critical,
        CharacterStat.MagicFind, CharacterStat.HealthRecovery, CharacterStat.ManaRecovery, CharacterStat.MoveSpeed,
        CharacterStat.GatheringSpeed, CharacterStat.PhysicalResist,
    ];

    private static readonly CharacterStat[] FairyStats =
    [
        CharacterStat.Strength, CharacterStat.Agility, CharacterStat.Accuracy, CharacterStat.Constitution,
        CharacterStat.Spirit,
    ];

    private static readonly Dictionary<CharacterStat, string> StatNames = new()
    {
        [CharacterStat.Strength] = "Strength",
        [CharacterStat.Agility] = "Agility",
        [CharacterStat.Accuracy] = "Accuracy",
        [CharacterStat.Constitution] = "Constitution",
        [CharacterStat.Spirit] = "Spirit",
        [CharacterStat.Luck] = "Luck",
        [CharacterStat.AttackSpeed] = "Attack speed",
        [CharacterStat.AttackRange] = "Attack range",
        [CharacterStat.MinAttack] = "Min attack",
        [CharacterStat.MaxAttack] = "Max attack",
        [CharacterStat.Defense] = "Defense",
        [CharacterStat.MaxHealth] = "Max HP",
        [CharacterStat.MaxMana] = "Max SP",
        [CharacterStat.Dodge] = "Dodge",
        [CharacterStat.Hit] = "Hit rate",
        [CharacterStat.Critical] = "Critical",
        [CharacterStat.MagicFind] = "Magic find",
        [CharacterStat.HealthRecovery] = "HP recovery",
        [CharacterStat.ManaRecovery] = "SP recovery",
        [CharacterStat.MoveSpeed] = "Move speed",
        [CharacterStat.GatheringSpeed] = "Gathering speed",
        [CharacterStat.PhysicalResist] = "Physical resist",
    };

    private static readonly Dictionary<EquipmentSlot, string> GearSlotNames = new()
    {
        [EquipmentSlot.Head] = "Head",
        [EquipmentSlot.Face] = "Face",
        [EquipmentSlot.Body] = "Body",
        [EquipmentSlot.Gloves] = "Gloves",
        [EquipmentSlot.Shoes] = "Shoes",
        [EquipmentSlot.Necklace] = "Necklace",
        [EquipmentSlot.LeftHand] = "Left hand",
        [EquipmentSlot.Ring1] = "Ring 1",
        [EquipmentSlot.Ring2] = "Ring 2",
        [EquipmentSlot.RightHand] = "Right hand",
        [EquipmentSlot.Bracelet1] = "Bracelet 1",
        [EquipmentSlot.Bracelet2] = "Bracelet 2",
        [EquipmentSlot.Belt] = "Belt",
        [EquipmentSlot.Handguard] = "Handguard",
        [EquipmentSlot.Wings] = "Wings",
        [EquipmentSlot.Cloak] = "Cloak",
        [EquipmentSlot.Fairy] = "Fairy",
        [EquipmentSlot.RearFairy] = "Rear fairy",
        [EquipmentSlot.Mount] = "Mount",
    };

    private static readonly Dictionary<EquipmentSlot, string> ApparelSlotNames = new()
    {
        [EquipmentSlot.HeadApparel] = "Head",
        [EquipmentSlot.FaceApparel] = "Face",
        [EquipmentSlot.BodyApparel] = "Body",
        [EquipmentSlot.GlovesApparel] = "Gloves",
        [EquipmentSlot.ShoesApparel] = "Shoes",
        [EquipmentSlot.FairyApparel] = "Fairy",
        [EquipmentSlot.GlowApparel] = "Glow",
        [EquipmentSlot.DaggerApparel] = "Dagger",
        [EquipmentSlot.GunApparel] = "Gun",
        [EquipmentSlot.SwordApparel] = "Sword",
        [EquipmentSlot.GreatswordApparel] = "Greatsword",
        [EquipmentSlot.StaffApparel] = "Staff",
        [EquipmentSlot.BowApparel] = "Bow",
        [EquipmentSlot.OffHandSwordApparel] = "Off-hand sword",
        [EquipmentSlot.ShieldApparel] = "Shield",
    };

    public ItemDescription Describe(ItemEntry item, BagItem instance, ItemPlace place, CharacterState wearer,
        DateTimeOffset now)
    {
        var forge = ReadForge(instance.ForgeParameter);
        var stats = new ItemStats(item, instance);
        var rows = new List<ItemRow>();
        var level = (int?)null;
        var fits = (IReadOnlyList<string>)[];
        var gemLevels = (IReadOnlyList<ItemGemLevel>)[];
        var text = item.Description is { Length: > 0 } description ? description : null;
        var isApparel = IsApparel(instance);

        if (isApparel)
        {
            text = null;

            AddBodyRequirement(rows, item, wearer);
        }
        else if (IsGear(item.Type))
        {
            level = ItemLevelOf(instance) is > 0 and var itemLevel ? itemLevel : null;
            text = null;

            AddGearStats(rows, item, instance, stats);
            AddRequirements(rows, item, instance, wearer);
        }
        else if (FairyTypes.Contains(item.Type))
        {
            level = FairyStats.Sum(stat => instance.InstanceStats.GetValueOrDefault(stat)) is > 0 and var fairyLevel
                ? fairyLevel
                : null;

            var isGrowthFull = instance.MaxEnergy > 0 && instance.Energy >= instance.MaxEnergy;

            rows.Add(new ItemRow("Stamina", OutOf(instance.Durability / DurabilityScale,
                instance.MaxDurability / DurabilityScale), ItemRowKind.Stamina));
            rows.Add(new ItemRow("Growth", OutOf(instance.Energy, instance.MaxEnergy),
                isGrowthFull ? ItemRowKind.FullGrowth : ItemRowKind.Growth));
        }
        else if (item.Type is GemType or RefiningGemType)
        {
            var gem = FindGem(item.Id);

            level = instance.MaxEnergy;
            fits = NameFits(gem);

            if (gem?.Bonus is { } bonus)
            {
                gemLevels = ListGemLevels(gem, bonus, instance.MaxEnergy);
                level = null;
                text = null;
            }
        }
        else if (ToolTypes.Contains(item.Type))
        {
            AddLevelRequirement(rows, item.RequiredLevel, wearer);
        }

        if (!isApparel && item.Type != NoBonusType && item.Type is not (GemType or RefiningGemType))
        {
            AddBonuses(rows, stats);
        }

        return new ItemDescription(
            Name: isApparel ? item.Name : NameOf(item, instance, forge),
            NameTone: ToneOf(item, instance),
            GemColor: GemColorOf(item),
            Level: level,
            TypeLine: TypeLineOf(item),
            Text: text,
            Fits: fits,
            GemLevels: gemLevels,
            Rows: rows,
            Sockets: forge.Sockets,
            Restrictions: RestrictionsOf(item, instance),
            Stack: item.StackLimit > 1 ? OutOf(instance.Count, item.StackLimit) : null,
            TradeValue: item.Price != 0 ? (place == ItemPlace.Equipment ? item.Price : item.Price / 2) : null,
            IsLocked: instance.IsLocked,
            Expiry: ExpiryOf(instance.Expiration, now));
    }

    public static string SlotNameOf(EquipmentSlot slot)
    {
        return ApparelSlotNames.TryGetValue(slot, out var apparelName)
            ? $"Apparel · {apparelName}"
            : GearSlotNames.GetValueOrDefault(slot, string.Empty);
    }

    public ItemNameTone ToneOf(ItemEntry item, BagItem instance)
    {
        return IsApparel(instance) ? ItemNameTone.White : NameToneOf(item, instance);
    }

    public static int? RarityOf(ItemEntry item, BagItem instance)
    {
        return IsRanked(item.Type) && !IsApparel(instance) ? RankEnergyOf(instance) / 100 % 10 : null;
    }

    public static ItemRestrictions RestrictionsOf(ItemEntry item, BagItem instance)
    {
        var restrictions = ItemRestrictions.None;

        if (!instance.IsTradable || !item.Tradeable)
        {
            restrictions |= ItemRestrictions.Trade;
        }

        if (!instance.IsTradable || !item.Discardable || instance.IsLocked)
        {
            restrictions |= ItemRestrictions.Throw;
        }

        if (!item.Deletable || instance.IsLocked)
        {
            restrictions |= ItemRestrictions.Delete;
        }

        return restrictions;
    }

    public int GemColorOf(ItemEntry item)
    {
        return item.Type == GemType ? FindGem(item.Id)?.Color ?? 0 : 0;
    }

    private static bool IsApparel(BagItem instance)
    {
        return instance.MaxDurability == ApparelDurability && instance.MaxEnergy == 0;
    }

    private static bool IsGear(int type)
    {
        return WeaponTypes.Contains(type) || DefenceTypes.Contains(type) || JewelleryTypes.Contains(type)
               || ArmourTypes.Contains(type);
    }

    private static bool IsRanked(int type)
    {
        return (type <= LastRankedType || type == WingType) && !UnrankedGearTypes.Contains(type);
    }

    private static int ItemLevelOf(BagItem instance)
    {
        return unchecked((sbyte)(instance.InstanceParameter & 0xFF));
    }

    private static int RankEnergyOf(BagItem instance)
    {
        return Math.Max(instance.MaxEnergy, 0) / 10;
    }

    private static void AddGearStats(List<ItemRow> rows, ItemEntry item, BagItem instance, ItemStats stats)
    {
        if (WeaponTypes.Contains(item.Type))
        {
            var attack = string.Create(CultureInfo.InvariantCulture,
                $"{stats.Take(CharacterStat.MinAttack)} – {stats.Take(CharacterStat.MaxAttack)}");

            rows.Add(new ItemRow("Attack", attack, ItemRowKind.Stat));
        }
        else if (DefenceTypes.Contains(item.Type) || ArmourTypes.Contains(item.Type))
        {
            AddStat(rows, "Defense", stats.Take(CharacterStat.Defense));
        }

        if (!JewelleryTypes.Contains(item.Type))
        {
            var isWorn = instance.MaxDurability > 0
                         && (double)instance.Durability / instance.MaxDurability < WornDurability;

            rows.Add(new ItemRow("Durability", OutOf(instance.Durability / DurabilityScale,
                instance.MaxDurability / DurabilityScale), isWorn ? ItemRowKind.WornStat : ItemRowKind.Stat));
        }

        if (DefenceTypes.Contains(item.Type))
        {
            AddStat(rows, "Physical resist", stats.Take(CharacterStat.PhysicalResist));
        }
        else if (item.Type == HitGearType)
        {
            AddStat(rows, "Hit rate", stats.Take(CharacterStat.Hit));
        }
        else if (item.Type == DodgeGearType)
        {
            AddStat(rows, "Dodge", stats.Take(CharacterStat.Dodge));
        }

        if (ItemLevelOf(instance) is > 0 and var itemLevel)
        {
            rows.Add(new ItemRow("Effectiveness", $"{itemLevel}%", ItemRowKind.Stat));
        }
    }

    private static void AddStat(List<ItemRow> rows, string key, int value)
    {
        if (value != 0)
        {
            rows.Add(new ItemRow(key, value.ToString(CultureInfo.InvariantCulture), ItemRowKind.Stat));
        }
    }

    private void AddRequirements(List<ItemRow> rows, ItemEntry item, BagItem instance, CharacterState wearer)
    {
        AddLevelRequirement(rows, instance.NeedLevel != 0 ? instance.NeedLevel : item.RequiredLevel, wearer);
        AddBodyRequirement(rows, item, wearer);

        if (item.Jobs.Length > 0 && item.Jobs[0] != AnyJob)
        {
            rows.Add(new ItemRow("Class", string.Join(", ", item.Jobs.Select(jobs.NameOf)),
                RequirementKind(item.Jobs.Contains(wearer.Job))));
        }
    }

    private static void AddBodyRequirement(List<ItemRow> rows, ItemEntry item, CharacterState wearer)
    {
        if (item.Bodies.Length > 0)
        {
            rows.Add(new ItemRow("Character", string.Join(", ", item.Bodies.Select(BodyNameOf)),
                RequirementKind(item.Bodies.Contains(wearer.TypeId))));
        }
    }

    private static void AddLevelRequirement(List<ItemRow> rows, int level, CharacterState wearer)
    {
        if (level != 0)
        {
            rows.Add(new ItemRow("Requires level", level.ToString(CultureInfo.InvariantCulture),
                RequirementKind(wearer.Level >= level)));
        }
    }

    private static ItemRowKind RequirementKind(bool isMet)
    {
        return isMet ? ItemRowKind.Requirement : ItemRowKind.UnmetRequirement;
    }

    private static string BodyNameOf(int body)
    {
        return body >= 1 && body <= BodyNames.Length ? BodyNames[body - 1] : string.Empty;
    }

    private static void AddBonuses(List<ItemRow> rows, ItemStats stats)
    {
        foreach (var stat in StatOrder)
        {
            if (stats.CoefficientOf(stat) is not 0 and var coefficient)
            {
                rows.Add(new ItemRow(StatNames[stat], PercentText(coefficient), ItemRowKind.Bonus));
            }
        }

        foreach (var stat in StatOrder)
        {
            if (stats.Take(stat) is not 0 and var value)
            {
                rows.Add(new ItemRow(StatNames[stat], value.ToString("+0;-0", CultureInfo.InvariantCulture),
                    ItemRowKind.Bonus));
            }
        }
    }

    private static string PercentText(int tenths)
    {
        return (tenths / 10.0).ToString("+0.#;-0.#", CultureInfo.InvariantCulture) + "%";
    }

    private static string OutOf(int count, int limit)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{count} / {limit}");
    }

    private string NameOf(ItemEntry item, BagItem instance, ItemForge forge)
    {
        var name = item.Name;

        if (IsRanked(item.Type) && prefixes.TryGetById(RankEnergyOf(instance) % 100, out var prefix))
        {
            name = $"{prefix.Name} {name}";
        }

        return forge.Level > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{name} +{forge.Level}")
            : name;
    }

    private static ItemNameTone NameToneOf(ItemEntry item, BagItem instance)
    {
        if (item.Type == GemType)
        {
            return ItemNameTone.Gem;
        }

        if (item.Type == QuestItemType)
        {
            return ItemNameTone.Blue;
        }

        return RarityOf(item, instance) switch
        {
            null => ItemNameTone.White,
            0 => ItemNameTone.Grey,
            1 or 2 => ItemNameTone.White,
            3 or 4 => ItemNameTone.Green,
            5 or 6 => ItemNameTone.Purple,
            7 or 8 => ItemNameTone.RedOrange,
            _ => ItemNameTone.Gold,
        };
    }

    private string? TypeLineOf(ItemEntry item)
    {
        if (item.EquipLinks is { Length: > 0 } links
            && links.All(link => ApparelSlotNames.ContainsKey((EquipmentSlot)link)))
        {
            return SlotNameOf((EquipmentSlot)links[0]);
        }

        return itemTypes.TryGetById(item.Type, out var type) ? type.Name : null;
    }

    private static ItemExpiry? ExpiryOf(int expiration, DateTimeOffset now)
    {
        if (expiration == 0)
        {
            return null;
        }

        var remaining = expiration - now.ToUnixTimeSeconds();

        if (remaining <= 0)
        {
            return new ItemExpiry("Item expired", ItemExpiryStage.Expired);
        }

        var days = remaining / SecondsPerDay;
        var clock = TimeSpan.FromSeconds(remaining % SecondsPerDay)
            .ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

        return days >= 1
            ? new ItemExpiry(string.Create(CultureInfo.InvariantCulture, $"{days} day(s), {clock} remaining"),
                ItemExpiryStage.Running)
            : new ItemExpiry($"{clock} remaining", ItemExpiryStage.LastDay);
    }

    private ItemForge ReadForge(int forgeParameter)
    {
        var value = unchecked((uint)forgeParameter);

        if (value == 0)
        {
            return new ItemForge([], 0);
        }

        var gemSlots = new List<ItemSocket>();
        var level = 0;

        for (var slot = 0; slot < GemSlotCount; slot++)
        {
            var gemData = value / (uint)Math.Pow(GemSlotBase, GemSlotCount - 1 - slot) % GemSlotBase;
            var gemLevel = (int)(gemData % 10);

            if (!gems.TryGetById((int)(gemData / 10), out var gem))
            {
                continue;
            }

            level += gemLevel;
            gemSlots.Add(new ItemSocket(string.Create(CultureInfo.InvariantCulture, $"Lv {gemLevel} {gem.Name}"),
                gem.Color, gem.Bonus != null ? BonusText(gem.Bonus, gemLevel) : null));
        }

        var sockets = Enumerable.Range(0, (int)(value / SocketDivisor))
            .Select(socket => socket < gemSlots.Count ? gemSlots[socket] : null)
            .ToArray();

        return new ItemForge(sockets, level);
    }

    private IReadOnlyList<string> NameFits(GemEntry? gem)
    {
        return (gem?.Fits ?? [])
            .Select(type => itemTypes.TryGetById(type, out var itemType) ? itemType.Name : null)
            .OfType<string>()
            .ToArray();
    }

    private static ItemGemLevel[] ListGemLevels(GemEntry gem, GemBonus bonus, int gemLevel)
    {
        var first = Math.Max(1, Math.Min(gemLevel - GemLadderReach, gem.MaxLevel - GemLadderReach * 2));
        var last = Math.Min(gem.MaxLevel, first + GemLadderReach * 2);

        return Enumerable.Range(first, Math.Max(last - first + 1, 0))
            .Select(ladderLevel =>
                new ItemGemLevel(ladderLevel, BonusText(bonus, ladderLevel), ladderLevel == gemLevel))
            .ToArray();
    }

    private static string BonusText(GemBonus bonus, int gemLevel)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{bonus.Text}{bonus.PerLevel * gemLevel}");
    }

    private GemEntry? FindGem(int itemId)
    {
        return gems.FirstOrDefault(gem => gem.ItemId == itemId);
    }

    private record ItemForge(IReadOnlyList<ItemSocket?> Sockets, int Level);

    private class ItemStats(ItemEntry item, BagItem instance)
    {
        private readonly HashSet<CharacterStat> _taken = [];

        public int CoefficientOf(CharacterStat stat)
        {
            return item.Stats.TryGetValue(stat, out var bonus) ? bonus.Coefficient : 0;
        }

        public int Take(CharacterStat stat)
        {
            if (!_taken.Add(stat))
            {
                return 0;
            }

            if (instance.InstanceStats.TryGetValue(stat, out var value))
            {
                return value;
            }

            return item.Stats.TryGetValue(stat, out var bonus) ? bonus.Minimum : 0;
        }
    }
}
