using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Features.Inventory;
using Top.Client.Features.Inventory.Tooltip;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;
using Top.Contracts;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;

namespace Top.Client.Tests.Features.Inventory.Tooltip;

public class ItemDescriberTests
{
    private const int Lance = 1;

    private const int Phyllis = 3;

    private const int Swordsman = 1;

    private const int Hunter = 2;

    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    private static readonly ItemDescriber Describer = new(
        new ItemTypeTable([
            new ItemTypeEntry { Id = 1, Name = "Sword" },
            new ItemTypeEntry { Id = 22, Name = "Body armor" },
            new ItemTypeEntry { Id = 23, Name = "Gloves" },
            new ItemTypeEntry { Id = 26, Name = "Ring" },
            new ItemTypeEntry { Id = 31, Name = "Consumable" },
            new ItemTypeEntry { Id = 49, Name = "Gem" },
        ]),
        new ItemPrefixTable([new ItemPrefixEntry { Id = 36, Name = "Uncommon" }]),
        new GemTable([
            new GemEntry
            {
                Id = 1, Name = "Fiery Gem", ItemId = 878, Color = 0xF6D243, Fits = [1, 22, 58], MaxLevel = 9,
                Bonus = new GemBonus { Text = "Attack +", PerLevel = 4 },
            },
            new GemEntry { Id = 2, Name = "Key of Apollo", ItemId = 2911, Color = 0xFFA500, MaxLevel = 9 },
            new GemEntry
            {
                Id = 3, Name = "Broken Gem", ItemId = 6817, Color = 0xFFA500, MaxLevel = 3,
                Bonus = new GemBonus { Text = "Agility +", PerLevel = 2 },
            },
        ]),
        new JobTable([
            new JobEntry { Id = Swordsman, Name = "Swordsman" }, new JobEntry { Id = Hunter, Name = "Hunter" },
            new JobEntry { Id = 8, Name = "Champion" },
        ]));

    private static readonly CharacterState Wearer = BuildWearer(Lance, level: 20, Swordsman);

    private static readonly ItemEntry Sword = new()
    {
        Id = 1, Name = "Short Sword", Type = 1, EquipLinks = [9, 6], Tradeable = true, Discardable = true,
        Deletable = true, StackLimit = 1, Price = 1015, RequiredLevel = 10, Jobs = [1, 8], Bodies = [1, 2],
        Stats = new Dictionary<CharacterStat, ItemStatBonus>
        {
            [CharacterStat.Strength] = new() { Minimum = 2, Maximum = 10 },
            [CharacterStat.AttackSpeed] = new() { Coefficient = 50 },
            [CharacterStat.Luck] = new() { Coefficient = 15 },
            [CharacterStat.MinAttack] = new() { Minimum = 30, Maximum = 58 },
            [CharacterStat.MaxAttack] = new() { Minimum = 38, Maximum = 70 },
        },
    };

    private static readonly ItemEntry Gloves = new()
    {
        Id = 2, Name = "Leather Gloves", Type = 23, EquipLinks = [3], Tradeable = true, Discardable = true,
        Deletable = true, StackLimit = 1,
        Stats = new Dictionary<CharacterStat, ItemStatBonus>
        {
            [CharacterStat.Defense] = new() { Minimum = 4, Maximum = 6 },
            [CharacterStat.Hit] = new() { Minimum = 3, Maximum = 5 },
        },
    };

    private static readonly ItemEntry Ring = new()
    {
        Id = 3, Name = "Final Perseverance", Type = 26, EquipLinks = [7, 8], Tradeable = true, Deletable = true,
        StackLimit = 1,
    };

    private static readonly ItemEntry Robe = new()
    {
        Id = 5343, Name = "Alluring Grand Robe", Type = 22, EquipLinks = [21], StackLimit = 1,
    };

    private static readonly ItemEntry Potion = new()
    {
        Id = 1847, Name = "Elven Fruit", Type = 31, EquipLinks = [-1], Tradeable = true, Discardable = true,
        Deletable = true, StackLimit = 99, Price = 50, Description = "Restores 150 SP.",
    };

    private static readonly ItemEntry FieryGem = new()
    {
        Id = 878, Name = "Fiery Gem", Type = 49, StackLimit = 99, Description = "Socket into a weapon.",
    };

    private static readonly ItemEntry Pouch = new() { Id = 949, Name = "Herbal Pouch", Type = 42, StackLimit = 99 };

    [TestCase(0, ItemNameTone.Grey)]
    [TestCase(1_000, ItemNameTone.White)]
    [TestCase(2_990, ItemNameTone.White)]
    [TestCase(3_000, ItemNameTone.Green)]
    [TestCase(4_500, ItemNameTone.Green)]
    [TestCase(5_000, ItemNameTone.Purple)]
    [TestCase(6_000, ItemNameTone.Purple)]
    [TestCase(7_000, ItemNameTone.RedOrange)]
    [TestCase(8_000, ItemNameTone.RedOrange)]
    [TestCase(9_000, ItemNameTone.Gold)]
    [TestCase(19_000, ItemNameTone.Gold)]
    public void Gear_names_itself_in_the_colour_of_the_hundreds_digit_of_its_energy_over_10(int maxEnergy,
        ItemNameTone tone)
    {
        Assert.That(Describe(Sword, BuildItem(Sword, maxEnergy: maxEnergy)).NameTone, Is.EqualTo(tone));
    }

    [Test]
    public void The_tone_and_gem_colour_of_an_item_are_those_of_its_name()
    {
        (ItemEntry Item, BagItem Instance)[] items =
        [
            (Sword, BuildItem(Sword, maxEnergy: 5_000)), (Sword, BuildItem(Sword)), (Potion, BuildItem(Potion)),
            (Pouch, BuildItem(Pouch)), (FieryGem, BuildItem(FieryGem, maxEnergy: 3)),
            (Sword, BuildItem(Sword, durability: 25_000, maxDurability: 25_000, maxEnergy: 0)),
        ];

        foreach (var (item, instance) in items)
        {
            var description = Describe(item, instance);

            Assert.That((Describer.ToneOf(item, instance), Describer.GemColorOf(item)),
                Is.EqualTo((description.NameTone, description.GemColor)), item.Name);
        }
    }

    [Test]
    public void The_last_two_digits_of_the_energy_over_10_name_the_prefix_of_gear()
    {
        var description = Describe(Sword, BuildItem(Sword, maxEnergy: 3_360));

        Assert.That(description.Name, Is.EqualTo("Uncommon Short Sword"));
        Assert.That(description.NameTone, Is.EqualTo(ItemNameTone.Green));
    }

    [Test]
    public void Gear_whose_prefix_digits_name_no_prefix_keeps_its_own_name()
    {
        Assert.That(Describe(Sword, BuildItem(Sword, maxEnergy: 3_450)).Name, Is.EqualTo("Short Sword"));
    }

    [Test]
    public void An_item_that_is_not_gear_is_white_whatever_its_energy()
    {
        var description = Describe(Potion, BuildItem(Potion, maxEnergy: 9_360));

        Assert.That(description.Name, Is.EqualTo("Elven Fruit"));
        Assert.That(description.NameTone, Is.EqualTo(ItemNameTone.White));
    }

    [Test]
    public void A_quest_item_is_named_in_blue()
    {
        Assert.That(Describe(Pouch, BuildItem(Pouch)).NameTone, Is.EqualTo(ItemNameTone.Blue));
    }

    [Test]
    public void A_gem_is_named_in_its_own_colour_and_its_ladder_shows_its_level()
    {
        var description = Describe(FieryGem, BuildItem(FieryGem, maxEnergy: 3));

        Assert.That(description.NameTone, Is.EqualTo(ItemNameTone.Gem));
        Assert.That(description.GemColor, Is.EqualTo(0xF6D243));
        Assert.That(description.Level, Is.Null, "the lit ladder row shows the level");
        Assert.That(description.Rows, Is.Empty, "the level ladder shows the gem bonus");
        Assert.That(description.Text, Is.Null, "the level ladder replaces the description");
    }

    [Test]
    public void A_gem_lists_its_bonus_at_its_own_level_and_the_two_levels_on_each_side()
    {
        Assert.That(Describe(FieryGem, BuildItem(FieryGem, maxEnergy: 4)).GemLevels, Is.EqualTo(new[]
        {
            new ItemGemLevel(2, "Attack +8", false), new ItemGemLevel(3, "Attack +12", false),
            new ItemGemLevel(4, "Attack +16", true), new ItemGemLevel(5, "Attack +20", false),
            new ItemGemLevel(6, "Attack +24", false),
        }));
    }

    [TestCase(1, 1, 5)]
    [TestCase(2, 1, 5)]
    [TestCase(8, 5, 9)]
    [TestCase(9, 5, 9)]
    public void A_gem_near_the_first_or_the_top_level_still_lists_five_levels(int level, int first, int last)
    {
        var levels = Describe(FieryGem, BuildItem(FieryGem, maxEnergy: level)).GemLevels;

        Assert.That(levels.Select(gemLevel => gemLevel.Level), Is.EqualTo(Enumerable.Range(first, last - first + 1)));
        Assert.That(levels.Single(gemLevel => gemLevel.IsCurrent).Level, Is.EqualTo(level));
    }

    [Test]
    public void A_gem_with_fewer_than_five_levels_lists_them_all()
    {
        var brokenGem = new ItemEntry { Id = 6817, Name = "Broken Gem", Type = 49 };

        Assert.That(Describe(brokenGem, BuildItem(brokenGem, maxEnergy: 2)).GemLevels.Select(level => level.Level),
            Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void A_gem_with_no_bonus_lists_no_levels_and_keeps_its_description()
    {
        var key = new ItemEntry { Id = 2911, Name = "Key of Apollo", Type = 49, Description = "Opens the black box." };

        var description = Describe(key, BuildItem(key, maxEnergy: 1));

        Assert.That(description.GemLevels, Is.Empty);
        Assert.That(description.Level, Is.EqualTo(1), "the energy of a gem is its level");
        Assert.That(description.Text, Is.EqualTo("Opens the black box."));
    }

    [Test]
    public void A_gem_names_the_item_types_it_fits_and_leaves_out_a_type_with_no_name()
    {
        Assert.That(Describe(FieryGem, BuildItem(FieryGem, maxEnergy: 1)).Fits,
            Is.EqualTo(new[] { "Sword", "Body armor" }));
        Assert.That(Describe(Sword, BuildItem(Sword)).Fits, Is.Empty, "only a gem fits into an item");
    }

    [Test]
    public void A_fairy_shows_its_stamina_and_its_growth_and_its_level_is_the_sum_of_its_attributes()
    {
        var fairy = new ItemEntry { Id = 183, Name = "Fairy of Luck", Type = 59, EquipLinks = [16] };
        var stats = new Dictionary<CharacterStat, int>
        {
            [CharacterStat.Strength] = 3, [CharacterStat.Agility] = 1, [CharacterStat.Spirit] = 2,
        };
        var item = new BagItem(fairy.Id, 0, 1, 12_000, 25_000, 90, 240, 0, true, true, false, 0, 0, 0, stats);

        var description = Describe(fairy, item);

        Assert.That(description.Level, Is.EqualTo(6));
        Assert.That(description.Rows.Take(2), Is.EqualTo(new[]
        {
            new ItemRow("Stamina", "240 / 500", ItemRowKind.Stamina),
            new ItemRow("Growth", "90 / 240", ItemRowKind.Growth),
        }));
    }

    [Test]
    public void A_fairy_with_full_growth_shows_it_as_full()
    {
        var fairy = new ItemEntry { Id = 183, Name = "Fairy of Luck", Type = 59, EquipLinks = [16] };
        var item = new BagItem(fairy.Id, 0, 1, 25_000, 25_000, 240, 240, 0, true, true, false, 0, 0, 0,
            new Dictionary<CharacterStat, int>());

        Assert.That(Describe(fairy, item).Rows[1], Is.EqualTo(new ItemRow("Growth", "240 / 240", ItemRowKind.FullGrowth)));
        Assert.That(Describe(fairy, item).Level, Is.Null, "a fairy with no attributes shows no level");
    }

    [Test]
    public void The_type_line_names_the_item_type()
    {
        Assert.That(Describe(Sword, BuildItem(Sword)).TypeLine, Is.EqualTo("Sword"));
        Assert.That(Describe(Potion, BuildItem(Potion)).TypeLine, Is.EqualTo("Consumable"));
    }

    [Test]
    public void The_type_line_of_an_apparel_item_names_the_apparel_slot_it_takes()
    {
        Assert.That(Describe(Robe, BuildItem(Robe)).TypeLine, Is.EqualTo("Apparel · Body"));
    }

    [TestCase(EquipmentSlot.RightHand, "Right hand")]
    [TestCase(EquipmentSlot.LeftHand, "Left hand")]
    [TestCase(EquipmentSlot.Ring2, "Ring 2")]
    [TestCase(EquipmentSlot.Bracelet1, "Bracelet 1")]
    [TestCase(EquipmentSlot.RearFairy, "Rear fairy")]
    [TestCase(EquipmentSlot.BodyApparel, "Apparel · Body")]
    public void An_equipment_slot_has_a_name(EquipmentSlot slot, string name)
    {
        Assert.That(ItemDescriber.SlotNameOf(slot), Is.EqualTo(name));
    }

    [Test]
    public void Every_equipment_slot_has_a_name()
    {
        Assert.That(Enum.GetValues<EquipmentSlot>().Select(ItemDescriber.SlotNameOf), Has.None.Empty);
    }

    [Test]
    public void Apparel_shows_no_stats_and_only_the_characters_that_can_wear_it()
    {
        var robe = new ItemEntry
        {
            Id = 5341, Name = "Robe of the Chilling Heart", Type = 22, EquipLinks = [21], Bodies = [1, 3],
            RequiredLevel = 40, Jobs = [8],
            Stats = new Dictionary<CharacterStat, ItemStatBonus> { [CharacterStat.Defense] = new() { Minimum = 9 } },
        };
        var worn = BuildItem(robe, durability: 25_000, maxDurability: 25_000, maxEnergy: 0, instanceParameter: 5);

        var description = Describe(robe, worn);

        Assert.That(description.NameTone, Is.EqualTo(ItemNameTone.White));
        Assert.That(description.Level, Is.Null);
        Assert.That(description.Rows, Is.EqualTo(new[]
        {
            new ItemRow("Character", "Lance, Phyllis", ItemRowKind.Requirement),
        }));
    }

    [Test]
    public void An_item_type_with_no_name_has_no_type_line()
    {
        Assert.That(Describe(Pouch, BuildItem(Pouch)).TypeLine, Is.Null);
    }

    [Test]
    public void The_low_byte_of_the_instance_parameter_is_the_level_of_gear()
    {
        Assert.That(Describe(Sword, BuildItem(Sword, instanceParameter: 0x1_0007)).Level, Is.EqualTo(7));
        Assert.That(Describe(Sword, BuildItem(Sword)).Level, Is.Null, "level 0 shows no level");
    }

    [Test]
    public void A_weapon_lists_attack_durability_and_effectiveness_then_requirements_then_bonuses()
    {
        var sword = BuildItem(Sword, durability: 9_000, maxDurability: 10_000, instanceParameter: 7);

        Assert.That(Describe(Sword, sword).Rows, Is.EqualTo(new[]
        {
            new ItemRow("Attack", "30 – 38", ItemRowKind.Stat),
            new ItemRow("Durability", "180 / 200", ItemRowKind.Stat),
            new ItemRow("Effectiveness", "7%", ItemRowKind.Stat),
            new ItemRow("Requires level", "10", ItemRowKind.Requirement),
            new ItemRow("Character", "Lance, Carsise", ItemRowKind.Requirement),
            new ItemRow("Class", "Swordsman, Champion", ItemRowKind.Requirement),
            new ItemRow("Luck", "+1.5%", ItemRowKind.Bonus),
            new ItemRow("Attack speed", "+5%", ItemRowKind.Bonus),
            new ItemRow("Strength", "+2", ItemRowKind.Bonus),
        }));
    }

    [Test]
    public void An_instance_stat_replaces_the_table_value_of_that_stat()
    {
        var sword = BuildItem(Sword, stats: new Dictionary<CharacterStat, int>
        {
            [CharacterStat.MinAttack] = 44, [CharacterStat.MaxAttack] = 61, [CharacterStat.Strength] = 7,
            [CharacterStat.Dodge] = -2,
        });

        var rows = Describe(Sword, sword).Rows;

        Assert.That(rows[0], Is.EqualTo(new ItemRow("Attack", "44 – 61", ItemRowKind.Stat)));
        Assert.That(rows.Where(row => row.Kind == ItemRowKind.Bonus).Select(row => (row.Key, row.Value)), Is.EqualTo(
            new[] { ("Luck", "+1.5%"), ("Attack speed", "+5%"), ("Strength", "+7"), ("Dodge", "-2") }));
    }

    [Test]
    public void Durability_below_30_percent_is_a_worn_stat()
    {
        var row = Describe(Sword, BuildItem(Sword, durability: 2_950, maxDurability: 10_000)).Rows[1];

        Assert.That(row, Is.EqualTo(new ItemRow("Durability", "59 / 200", ItemRowKind.WornStat)));
    }

    [Test]
    public void Gloves_list_defense_durability_hit_rate_and_effectiveness()
    {
        var gloves = BuildItem(Gloves, durability: 5_000, maxDurability: 5_000, instanceParameter: 10);

        Assert.That(Describe(Gloves, gloves).Rows.Select(row => (row.Key, row.Value)), Is.EqualTo(new[]
        {
            ("Defense", "4"), ("Durability", "100 / 100"), ("Hit rate", "3"), ("Effectiveness", "10%"),
        }));
    }

    [Test]
    public void Gear_of_item_level_0_shows_no_effectiveness()
    {
        var rows = Describe(Gloves, BuildItem(Gloves, durability: 5_000, maxDurability: 5_000)).Rows;

        Assert.That(rows.Select(row => row.Key), Has.None.EqualTo("Effectiveness"));
    }

    [Test]
    public void Jewellery_shows_no_durability()
    {
        var rows = Describe(Ring, BuildItem(Ring, durability: 5_000, maxDurability: 5_000)).Rows;

        Assert.That(rows.Select(row => row.Key), Has.None.EqualTo("Durability"));
    }

    [Test]
    public void A_requirement_the_wearer_does_not_meet_is_unmet()
    {
        var wearer = BuildWearer(Phyllis, level: 9, Hunter);

        var requirements = Describe(Sword, BuildItem(Sword), wearer: wearer).Rows
            .Where(row => row.Kind is ItemRowKind.Requirement or ItemRowKind.UnmetRequirement);

        Assert.That(requirements.Select(row => (row.Key, row.Kind)), Is.EqualTo(new[]
        {
            ("Requires level", ItemRowKind.UnmetRequirement), ("Character", ItemRowKind.UnmetRequirement),
            ("Class", ItemRowKind.UnmetRequirement),
        }));
    }

    [Test]
    public void The_level_an_item_instance_needs_replaces_the_level_of_the_table()
    {
        var level = Describe(Sword, BuildItem(Sword, needLevel: 45)).Rows.Single(row => row.Key == "Requires level");

        Assert.That(level, Is.EqualTo(new ItemRow("Requires level", "45", ItemRowKind.UnmetRequirement)));
    }

    [Test]
    public void The_forge_parameter_lists_one_socket_per_hole_with_the_gems_in_them()
    {
        var sword = BuildItem(Sword, forgeParameter: 2_013_000_000);

        var description = Describe(Sword, sword);

        Assert.That(description.Sockets, Is.EqualTo(new[]
        {
            new ItemSocket("Lv 3 Fiery Gem", 0xF6D243, "Attack +12"), null,
        }));
        Assert.That(description.Name, Is.EqualTo("Short Sword +3"), "the gem levels add up to the forge level");
    }

    [Test]
    public void A_forge_parameter_of_three_holes_reads_past_the_signed_range()
    {
        var sword = BuildItem(Sword, forgeParameter: unchecked((int)3_013_021_000u));

        var description = Describe(Sword, sword);

        Assert.That(description.Sockets, Is.EqualTo(new[]
        {
            new ItemSocket("Lv 3 Fiery Gem", 0xF6D243, "Attack +12"),
            new ItemSocket("Lv 1 Key of Apollo", 0xFFA500, null),
            null,
        }));
        Assert.That(description.Name, Is.EqualTo("Short Sword +4"));
    }

    [Test]
    public void An_item_with_no_forge_parameter_has_no_sockets()
    {
        Assert.That(Describe(Sword, BuildItem(Sword)).Sockets, Is.Empty);
    }

    [Test]
    public void An_item_restricts_only_the_actions_it_does_not_allow()
    {
        Assert.That(Describe(Sword, BuildItem(Sword)).Restrictions, Is.EqualTo(ItemRestrictions.None));
        Assert.That(Describe(Pouch, BuildItem(Pouch)).Restrictions,
            Is.EqualTo(ItemRestrictions.Trade | ItemRestrictions.Throw | ItemRestrictions.Delete));
    }

    [Test]
    public void An_untradable_instance_can_not_be_traded_or_thrown()
    {
        Assert.That(Describe(Sword, BuildItem(Sword, isTradable: false)).Restrictions,
            Is.EqualTo(ItemRestrictions.Trade | ItemRestrictions.Throw));
    }

    [Test]
    public void A_locked_item_says_so_and_can_not_be_thrown_or_deleted()
    {
        var description = Describe(Sword, BuildItem(Sword, isLocked: true));

        Assert.That(description.IsLocked, Is.True);
        Assert.That(description.Restrictions, Is.EqualTo(ItemRestrictions.Throw | ItemRestrictions.Delete));
    }

    [Test]
    public void An_item_in_a_bag_trades_for_half_its_price_and_a_worn_item_for_all_of_it()
    {
        Assert.That(Describe(Sword, BuildItem(Sword)).TradeValue, Is.EqualTo(507));
        Assert.That(Describe(Sword, BuildItem(Sword), ItemPlace.TemporaryBag).TradeValue, Is.EqualTo(507));
        Assert.That(Describe(Sword, BuildItem(Sword), ItemPlace.Equipment).TradeValue, Is.EqualTo(1015));
        Assert.That(Describe(Pouch, BuildItem(Pouch)).TradeValue, Is.Null, "an item with no price has no value");
    }

    [Test]
    public void A_stackable_item_shows_its_count_out_of_its_stack_limit()
    {
        Assert.That(Describe(Potion, BuildItem(Potion, count: 12)).Stack, Is.EqualTo("12 / 99"));
        Assert.That(Describe(Sword, BuildItem(Sword)).Stack, Is.Null);
    }

    [Test]
    public void An_item_that_is_not_gear_shows_its_description_and_gear_does_not()
    {
        Assert.That(Describe(Potion, BuildItem(Potion)).Text, Is.EqualTo("Restores 150 SP."));
        Assert.That(Describe(Sword, BuildItem(Sword)).Text, Is.Null);
    }

    [Test]
    public void A_timed_item_counts_down_in_days_until_its_last_day()
    {
        var expiration = (int)Now.AddDays(2).AddHours(3).AddMinutes(4).AddSeconds(5).ToUnixTimeSeconds();

        Assert.That(Describe(Potion, BuildItem(Potion, expiration: expiration)).Expiry,
            Is.EqualTo(new ItemExpiry("2 day(s), 03:04:05 remaining", ItemExpiryStage.Running)));
    }

    [Test]
    public void A_timed_item_on_its_last_day_counts_down_in_hours()
    {
        var expiration = (int)Now.AddHours(3).AddMinutes(4).AddSeconds(5).ToUnixTimeSeconds();

        Assert.That(Describe(Potion, BuildItem(Potion, expiration: expiration)).Expiry,
            Is.EqualTo(new ItemExpiry("03:04:05 remaining", ItemExpiryStage.LastDay)));
    }

    [Test]
    public void A_timed_item_past_its_expiration_is_expired()
    {
        var expiration = (int)Now.ToUnixTimeSeconds();

        Assert.That(Describe(Potion, BuildItem(Potion, expiration: expiration)).Expiry,
            Is.EqualTo(new ItemExpiry("Item expired", ItemExpiryStage.Expired)));
        Assert.That(Describe(Potion, BuildItem(Potion)).Expiry, Is.Null, "expiration 0 is no expiration");
    }

    private static ItemDescription Describe(ItemEntry item, BagItem instance, ItemPlace place = ItemPlace.Bag,
        CharacterState? wearer = null)
    {
        return Describer.Describe(item, instance, place, wearer ?? Wearer, Now);
    }

    private static BagItem BuildItem(ItemEntry item, int count = 1, int needLevel = 0, int durability = 0,
        int maxDurability = 0, int maxEnergy = 0, bool isTradable = true, bool isLocked = false, int expiration = 0,
        int forgeParameter = 0, int instanceParameter = 0, Dictionary<CharacterStat, int>? stats = null)
    {
        return new BagItem(item.Id, needLevel, count, durability, maxDurability, 0, maxEnergy, 0, true, isTradable,
            isLocked, expiration, forgeParameter, instanceParameter, stats ?? new Dictionary<CharacterStat, int>());
    }

    private static CharacterState BuildWearer(int typeId, int level, int job)
    {
        return new CharacterState(1, CharacterControlType.Player, typeId, "Wearer", Vector2.Zero, 0, 0, 0, 0,
            string.Empty, string.Empty, 0, level, 100, 100, 20, 20, job, 0, 0, 5, 0, 0f, null, null);
    }
}
