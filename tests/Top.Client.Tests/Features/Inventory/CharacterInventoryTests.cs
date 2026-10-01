using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Client.Features.Inventory;
using Top.Client.Features.Inventory.Bag;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Contracts;
using Top.Contracts.Tables.Items;

namespace Top.Client.Tests.Features.Inventory;

public class CharacterInventoryTests
{
    private const uint PlayerId = 12;

    private const uint BoatId = 40;

    private const uint OtherPlayerId = 41;

    private const int RingInBagSlot = 2;

    private const int BraceletInBagSlot = 3;

    private static readonly BagItem Sword = BuildItem(289);

    private static readonly BagItem Apple = BuildItem(3_000, count: 20);

    private static readonly BagItem Rum = BuildItem(3_100, count: 5);

    private static readonly BagItem Vest = BuildItem(4_308);

    private static readonly BagItem Ring = BuildItem(4_200);

    private static readonly BagItem Bracelet = BuildItem(7_310);

    private static readonly BagItem Map = BuildItem(4_400);

    private static readonly BagItem Potion = BuildItem(1_847, count: 10);

    private static readonly BagItem Elixir = BuildItem(1_848, count: 3);

    private static readonly BagItem SkillBook = BuildItem(3_300);

    private static readonly BagItem PetFood = BuildItem(3_400);

    private static readonly BagItem Firework = BuildItem(1_849);

    private static readonly BagItem RefiningHammer = BuildItem(865);

    private static readonly BagItem ScriptedCutlass = BuildItem(3_500);

    private static readonly BagItem BoatFigurehead = BuildItem(3_600);

    private static readonly BagItem Letter = BuildItem(3_700);

    private static readonly DateTime Now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ItemTable Items = new([
        new ItemEntry
        {
            Id = Sword.ItemId, Name = "Short Sword", Type = 1, EquipLinks = [9, 6, -2], StackLimit = 1,
            Discardable = true, Deletable = true,
        },
        new ItemEntry
        {
            Id = Apple.ItemId, Name = "Apple", Type = 31, EquipLinks = [-1], Discardable = true, Deletable = true,
        },
        new ItemEntry
        {
            Id = Rum.ItemId, Name = "Rum", Type = 31, EquipLinks = [-1], StackLimit = 99, ScriptEffect = "ItemUse_Rum",
        },
        new ItemEntry { Id = Vest.ItemId, Name = "Sailor Vest", Type = 22, EquipLinks = [2, -2] },
        new ItemEntry { Id = Ring.ItemId, Name = "Ruby Ring", Type = 26, EquipLinks = [7, 8] },
        new ItemEntry { Id = Bracelet.ItemId, Name = "Jade Bracelet", Type = 81, EquipLinks = [10, 11] },
        new ItemEntry { Id = Map.ItemId, Name = "Treasure Map", Type = 42, EquipLinks = [-1] },
        new ItemEntry
        {
            Id = Potion.ItemId, Name = "Potion", Type = 31, EquipLinks = [-1], StackLimit = 99,
            ScriptEffect = "ItemUse_Potion", Cooldown = 1f,
        },
        new ItemEntry
        {
            Id = Elixir.ItemId, Name = "Elixir", Type = 31, EquipLinks = [-1], StackLimit = 99,
            ScriptEffect = "ItemUse_Elixir", Cooldown = 4f,
        },
        new ItemEntry
        {
            Id = SkillBook.ItemId, Name = "Skill Book", Type = 71, EquipLinks = [-1], ScriptEffect = "200",
        },
        new ItemEntry
        {
            Id = PetFood.ItemId, Name = "Pet Food", Type = 66, EquipLinks = [-1], ScriptEffect = "ItemUse_PetFood",
        },
        new ItemEntry
        {
            Id = Firework.ItemId, Name = "Firework", Type = 31, EquipLinks = [-1], ScriptEffect = "ItemUse_Firework",
        },
        new ItemEntry
        {
            Id = RefiningHammer.ItemId, Name = "Refining Hammer", Type = 31, EquipLinks = [-1],
            ScriptEffect = "ItemUse_Refine",
        },
        new ItemEntry
        {
            Id = ScriptedCutlass.ItemId, Name = "Scripted Cutlass", Type = 1, EquipLinks = [9, -1],
            ScriptEffect = "ItemUse_Cutlass",
        },
        new ItemEntry
        {
            Id = BoatFigurehead.ItemId, Name = "Boat Figurehead", Type = 31, EquipLinks = [9_999],
            ScriptEffect = "ItemUse_Figurehead",
        },
        new ItemEntry { Id = Letter.ItemId, Name = "Letter", Type = 31, ScriptEffect = "ItemUse_Letter" },
    ]);

    private CharacterInventory _inventory;

    [SetUp]
    public void EnterMap()
    {
        _inventory = new CharacterInventory(
            BuildMapEntered(new BagSync(24, [new BagSlot(0, Sword), new BagSlot(1, Apple)]),
                BuildEquipment((EquipmentSlot.Body, Vest), (EquipmentSlot.RightHand, Sword))));
    }

    [Test]
    public void A_new_inventory_holds_the_bag_the_map_entry_carried()
    {
        Assert.That(_inventory.BagCapacity, Is.EqualTo(24));
        Assert.That(_inventory.Bag.Take(3), Is.EqualTo(new[] { Sword, Apple, null }));
    }

    [Test]
    public void The_bag_has_48_slots_and_the_temporary_bag_16_all_empty_until_a_sync_fills_them()
    {
        Assert.That(_inventory.Bag, Has.Count.EqualTo(48));
        Assert.That(_inventory.Bag.Skip(2), Is.All.Null);
        Assert.That(_inventory.TemporaryBag, Has.Count.EqualTo(16).And.All.Null);
    }

    [Test]
    public void A_bag_change_replaces_the_slots_it_names_and_keeps_the_rest()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(5, Rum)])));

        Assert.That(_inventory.Bag[5], Is.EqualTo(Rum));
        Assert.That(_inventory.Bag.Take(2), Is.EqualTo(new[] { Sword, Apple }));
    }

    [Test]
    public void A_moved_item_leaves_its_old_slot_empty()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId,
            new BagSync(null, [new BagSlot(0, null), new BagSlot(6, Sword)])));

        Assert.That(_inventory.Bag[0], Is.Null);
        Assert.That(_inventory.Bag[6], Is.EqualTo(Sword));
    }

    [Test]
    public void A_bag_change_without_a_capacity_keeps_the_capacity()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [])));

        Assert.That(_inventory.BagCapacity, Is.EqualTo(24));
    }

    [Test]
    public void A_bag_change_with_a_capacity_changes_the_capacity()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(32, [])));

        Assert.That(_inventory.BagCapacity, Is.EqualTo(32));
    }

    [Test]
    public void A_bag_change_of_another_character_leaves_the_bag_alone()
    {
        _inventory.ChangeBag(new BagChangedEvent(BoatId, new BagSync(8, [new BagSlot(0, Rum)])));

        Assert.That(_inventory.Bag[0], Is.EqualTo(Sword), "a boat's hold syncs with the boat's world id");
        Assert.That(_inventory.BagCapacity, Is.EqualTo(24));
    }

    [Test]
    public void A_temporary_bag_change_replaces_the_slots_it_names()
    {
        _inventory.ChangeTemporaryBag(new TemporaryBagChangedEvent(
            new BagSync(null, [new BagSlot(0, Apple), new BagSlot(3, Rum)])));
        _inventory.ChangeTemporaryBag(new TemporaryBagChangedEvent(new BagSync(null, [new BagSlot(0, null)])));

        Assert.That(_inventory.TemporaryBag.Take(4), Is.EqualTo(new[] { null, null, null, Rum }));
        Assert.That(_inventory.Bag.Take(2), Is.EqualTo(new[] { Sword, Apple }), "the main bag does not change");
    }

    [Test]
    public void A_slot_past_the_end_of_a_bag_is_left_out()
    {
        _inventory.ChangeTemporaryBag(new TemporaryBagChangedEvent(new BagSync(32, [new BagSlot(20, Rum)])));
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(48, Rum)])));

        Assert.That(_inventory.TemporaryBag, Is.All.Null);
        Assert.That(_inventory.Bag, Has.None.EqualTo(Rum));
    }

    [Test]
    public void A_new_inventory_holds_the_equipment_the_map_entry_carried()
    {
        Assert.That(_inventory.Equipment, Has.Count.EqualTo(34));
        Assert.That(_inventory.Equipment[(int)EquipmentSlot.Body], Is.EqualTo(Vest));
        Assert.That(_inventory.Equipment[(int)EquipmentSlot.RightHand], Is.EqualTo(Sword));
        Assert.That(_inventory.Equipment.Count(item => item != null), Is.EqualTo(2));
    }

    [Test]
    public void A_look_change_of_the_player_replaces_every_equipment_slot()
    {
        _inventory.ChangeEquipment(BuildLookChanged(PlayerId, (EquipmentSlot.Body, Vest), (EquipmentSlot.Ring2, Ring)));

        Assert.That(_inventory.Equipment[(int)EquipmentSlot.Ring2], Is.EqualTo(Ring));
        Assert.That(_inventory.Equipment[(int)EquipmentSlot.RightHand], Is.Null, "the sword came off");
        Assert.That(_inventory.Equipment.Count(item => item != null), Is.EqualTo(2));
    }

    [Test]
    public void A_look_change_of_another_character_leaves_the_equipment_alone()
    {
        _inventory.ChangeEquipment(BuildLookChanged(OtherPlayerId, (EquipmentSlot.Ring1, Ring)));

        Assert.That(_inventory.Equipment[(int)EquipmentSlot.Ring1], Is.Null);
        Assert.That(_inventory.Equipment[(int)EquipmentSlot.RightHand], Is.EqualTo(Sword));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void A_new_inventory_holds_the_bag_lock_the_map_entry_carried(bool isLocked)
    {
        var inventory = new CharacterInventory(BuildMapEntered(new BagSync(24, []), BuildEquipment(), isLocked));

        Assert.That(inventory.IsBagLocked, Is.EqualTo(isLocked));
    }

    [Test]
    public void A_lock_leaves_the_bag_unlocked_until_the_server_reports_it_locked()
    {
        Assert.That(_inventory.LockBag(), Is.InstanceOf<LockBagCommand>());
        Assert.That(_inventory.IsBagLocked, Is.False);

        Assert.That(_inventory.ChangeBagLock(new BagLockReportedEvent(true)), Is.EqualTo(BagLockResult.Locked));
        Assert.That(_inventory.IsBagLocked, Is.True);
    }

    [Test]
    public void An_unlock_sends_the_code_and_waits_with_the_bag_locked_until_the_server_reports()
    {
        var inventory = BuildLockedInventory();

        Assert.That(inventory.UnlockBag("123456").Code, Is.EqualTo("123456"));
        Assert.That(inventory.IsUnlockingBag, Is.True);
        Assert.That(inventory.IsBagLocked, Is.True);
    }

    [Test]
    public void An_unlocked_report_after_an_unlock_unlocks_the_bag_and_ends_the_wait()
    {
        var inventory = BuildLockedInventory();
        inventory.UnlockBag("123456");

        Assert.That(inventory.ChangeBagLock(new BagLockReportedEvent(false)), Is.EqualTo(BagLockResult.Unlocked));
        Assert.That(inventory.IsBagLocked, Is.False);
        Assert.That(inventory.IsUnlockingBag, Is.False);
    }

    [Test]
    public void A_locked_report_after_an_unlock_rejects_the_code_and_ends_the_wait()
    {
        var inventory = BuildLockedInventory();
        inventory.UnlockBag("654321");

        Assert.That(inventory.ChangeBagLock(new BagLockReportedEvent(true)), Is.EqualTo(BagLockResult.CodeRejected),
            "the server has no wrong-code reply: the bag is still locked");
        Assert.That(inventory.IsBagLocked, Is.True);
        Assert.That(inventory.IsUnlockingBag, Is.False);
    }

    [Test]
    public void A_locked_report_with_no_unlock_waiting_rejects_no_code()
    {
        var inventory = BuildLockedInventory();
        inventory.UnlockBag("654321");
        inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(inventory.ChangeBagLock(new BagLockReportedEvent(true)), Is.EqualTo(BagLockResult.Locked));
    }

    [Test]
    public void Moving_a_bag_item_sends_its_whole_stack_to_the_other_slot()
    {
        var command = _inventory.MoveBagItem(1, 5)!;

        Assert.That((command.WorldId, command.SourceSlot, command.Count, command.DestinationSlot),
            Is.EqualTo((PlayerId, 1, 20, 5)));
    }

    [Test]
    public void Moving_a_bag_item_onto_another_item_sends_the_same_move_for_the_server_to_swap()
    {
        var command = _inventory.MoveBagItem(0, 1)!;

        Assert.That((command.SourceSlot, command.Count, command.DestinationSlot), Is.EqualTo((0, 1, 1)));
    }

    [Test]
    public void A_move_leaves_the_bag_as_it_is_until_the_server_syncs_it()
    {
        _inventory.MoveBagItem(1, 5);

        Assert.That(_inventory.Bag.Take(6), Is.EqualTo(new[] { Sword, Apple, null, null, null, null }));

        _inventory.ChangeBag(new BagChangedEvent(PlayerId,
            new BagSync(null, [new BagSlot(1, null), new BagSlot(5, Apple)])));

        Assert.That(_inventory.Bag.Take(6), Is.EqualTo(new[] { Sword, null, null, null, null, Apple }));
    }

    [TestCase(2, 5, TestName = "from an empty slot")]
    [TestCase(1, 1, TestName = "onto its own slot")]
    [TestCase(1, 24, TestName = "onto a slot past the capacity")]
    [TestCase(1, -1, TestName = "onto a slot before the bag")]
    [TestCase(30, 5, TestName = "from a slot past the capacity")]
    public void A_move_the_bag_cannot_make_sends_nothing(int source, int destination)
    {
        Assert.That(_inventory.CanMoveBagItem(source, destination), Is.False);
        Assert.That(_inventory.MoveBagItem(source, destination), Is.Null);
    }

    [Test]
    public void An_item_moves_from_a_filled_slot_of_an_unlocked_bag_inside_its_capacity()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(30, Rum)])));

        Assert.That(_inventory.CanMoveBagItemFrom(1), Is.True);
        Assert.That(_inventory.CanMoveBagItemFrom(2), Is.False, "an empty slot");
        Assert.That(_inventory.CanMoveBagItemFrom(30), Is.False, "a slot past the capacity");

        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanMoveBagItemFrom(1), Is.False, "a locked bag");
    }

    [Test]
    public void Splitting_a_stack_sends_the_part_to_move_and_leaves_the_rest_in_its_slot()
    {
        var command = _inventory.SplitBagItem(1, 7, 5)!;

        Assert.That((command.WorldId, command.SourceSlot, command.Count, command.DestinationSlot),
            Is.EqualTo((PlayerId, 1, 7, 5)));
        Assert.That(_inventory.Bag[1], Is.EqualTo(Apple), "the server syncs the split");
    }

    [Test]
    public void A_stack_splits_into_an_empty_slot_or_onto_a_stack_of_the_same_item()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId,
            new BagSync(null, [new BagSlot(3, Apple with { Count = 4 }), new BagSlot(4, Rum)])));

        Assert.That(_inventory.CanSplitBagItem(1, 5), Is.True, "an empty slot");
        Assert.That(_inventory.CanSplitBagItem(1, 3), Is.True, "a stack of the same item");
        Assert.That(_inventory.CanSplitBagItem(1, 4), Is.False, "a stack of another item");
        Assert.That(_inventory.CanSplitBagItem(0, 5), Is.False, "a single item");
        Assert.That(_inventory.SplitBagItem(1, 7, 4), Is.Null);
        Assert.That(_inventory.SplitBagItem(0, 1, 5), Is.Null);
    }

    [TestCase(0)]
    [TestCase(20)]
    [TestCase(21)]
    public void A_split_that_leaves_no_part_on_either_side_sends_nothing(int count)
    {
        Assert.That(_inventory.SplitBagItem(1, count, 5), Is.Null);
    }

    [Test]
    public void A_locked_bag_sends_no_move_and_no_split()
    {
        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanMoveBagItem(1, 5), Is.False);
        Assert.That(_inventory.MoveBagItem(1, 5), Is.Null);
        Assert.That(_inventory.CanSplitBagItem(1, 5), Is.False);
        Assert.That(_inventory.SplitBagItem(1, 7, 5), Is.Null);
    }

    [Test]
    public void A_bag_that_waits_for_its_unlock_sends_no_move()
    {
        var inventory = new CharacterInventory(
            BuildMapEntered(new BagSync(24, [new BagSlot(1, Apple)]), BuildEquipment(), isBagLocked: true));
        inventory.UnlockBag("123456");

        Assert.That(inventory.MoveBagItem(1, 5), Is.Null);
    }

    [Test]
    public void An_unlocked_bag_moves_again()
    {
        _inventory.ChangeBagLock(new BagLockReportedEvent(true));
        _inventory.ChangeBagLock(new BagLockReportedEvent(false));

        Assert.That(_inventory.MoveBagItem(1, 5), Is.Not.Null);
    }

    [Test]
    public void Equipping_a_bag_item_onto_the_slot_the_server_fills_first_lets_the_server_pick_it()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring);

        var command = inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring1, Items)!;

        Assert.That((command.WorldId, command.BagSlot, command.IsSecondSlot), Is.EqualTo((PlayerId, 2, false)));
    }

    [Test]
    public void Equipping_a_ring_onto_the_free_second_ring_slot_asks_for_the_second_slot()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring);

        Assert.That(inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring2, Items)!.IsSecondSlot, Is.True);
    }

    [Test]
    public void A_ring_with_the_first_ring_slot_full_fits_only_the_free_second_slot()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring1, Ring));

        Assert.That(inventory.CanEquipBagItem(RingInBagSlot, EquipmentSlot.Ring1, Items), Is.False,
            "the server fills the free second slot");
        Assert.That(inventory.CanEquipBagItem(RingInBagSlot, EquipmentSlot.Ring2, Items), Is.True);
        Assert.That(inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring2, Items)!.IsSecondSlot, Is.False);
    }

    [Test]
    public void A_ring_with_both_ring_slots_full_replaces_either_ring()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring1, Ring),
            (EquipmentSlot.Ring2, Ring));

        Assert.That(inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring1, Items)!.IsSecondSlot, Is.False);
        Assert.That(inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring2, Items)!.IsSecondSlot, Is.True);
    }

    [Test]
    public void A_bracelet_fits_only_the_slot_the_server_fills_first()
    {
        var empty = BuildInventoryWith(BraceletInBagSlot, Bracelet);
        var firstFull = BuildInventoryWith(BraceletInBagSlot, Bracelet, (EquipmentSlot.Bracelet1, Bracelet));
        var bothFull = BuildInventoryWith(BraceletInBagSlot, Bracelet, (EquipmentSlot.Bracelet1, Bracelet),
            (EquipmentSlot.Bracelet2, Bracelet));

        Assert.That(FittingSlots(empty, BraceletInBagSlot), Is.EqualTo(new[] { EquipmentSlot.Bracelet1 }));
        Assert.That(FittingSlots(firstFull, BraceletInBagSlot), Is.EqualTo(new[] { EquipmentSlot.Bracelet2 }));
        Assert.That(FittingSlots(bothFull, BraceletInBagSlot), Is.EqualTo(new[] { EquipmentSlot.Bracelet1 }));
    }

    [Test]
    public void A_sword_fits_the_main_hand_and_the_second_hand()
    {
        Assert.That(FittingSlots(_inventory, 0), Is.EqualTo(new[] { EquipmentSlot.LeftHand }),
            "the main hand holds a sword, so the server fills the free second hand");
        Assert.That(_inventory.EquipBagItem(0, EquipmentSlot.LeftHand, Items)!.IsSecondSlot, Is.False);

        var inventory = BuildInventoryWith(0, Sword);

        Assert.That(FittingSlots(inventory, 0), Is.EqualTo(new[] { EquipmentSlot.LeftHand, EquipmentSlot.RightHand }));
        Assert.That(inventory.EquipBagItem(0, EquipmentSlot.LeftHand, Items)!.IsSecondSlot, Is.True);
        Assert.That(inventory.EquipBagItem(0, EquipmentSlot.RightHand, Items)!.IsSecondSlot, Is.False);
    }

    [Test]
    public void The_filled_fitting_slots_of_a_ring_are_both_rings_when_both_are_full()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring1, Ring),
            (EquipmentSlot.Ring2, Ring));

        Assert.That(inventory.FilledFittingSlotsOf(EntryOf(Ring)),
            Is.EqualTo(new[] { EquipmentSlot.Ring1, EquipmentSlot.Ring2 }));
    }

    [Test]
    public void The_filled_fitting_slots_leave_out_an_empty_slot()
    {
        var firstFull = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring1, Ring));
        var secondFull = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring2, Ring));
        var empty = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Body, Vest));

        Assert.That(firstFull.FilledFittingSlotsOf(EntryOf(Ring)),
            Is.EqualTo(new[] { EquipmentSlot.Ring1 }));
        Assert.That(secondFull.FilledFittingSlotsOf(EntryOf(Ring)),
            Is.EqualTo(new[] { EquipmentSlot.Ring2 }));
        Assert.That(empty.FilledFittingSlotsOf(EntryOf(Ring)), Is.Empty);
    }

    [Test]
    public void An_item_that_is_not_equipment_has_no_filled_fitting_slot()
    {
        Assert.That(_inventory.FilledFittingSlotsOf(EntryOf(Apple)), Is.Empty);
    }

    [Test]
    public void An_item_that_is_not_equipment_fits_no_slot_and_sends_nothing()
    {
        Assert.That(FittingSlots(_inventory, 1), Is.Empty);
        Assert.That(_inventory.EquipBagItem(1, EquipmentSlot.Ring1, Items), Is.Null);
        Assert.That(_inventory.EquipBagItem(1, Items), Is.Null);
    }

    [Test]
    public void An_item_missing_from_the_item_table_fits_no_slot()
    {
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(3, BuildItem(9_999))])));

        Assert.That(FittingSlots(_inventory, 3), Is.Empty);
        Assert.That(_inventory.EquipBagItem(3, Items), Is.Null);
    }

    [Test]
    public void Equipping_without_a_slot_lets_the_server_replace_the_first_ring_when_both_are_full()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Ring1, Ring),
            (EquipmentSlot.Ring2, Ring));

        var command = inventory.EquipBagItem(RingInBagSlot, Items)!;

        Assert.That((command.WorldId, command.BagSlot, command.IsSecondSlot), Is.EqualTo((PlayerId, 2, false)));
    }

    [Test]
    public void An_equip_leaves_the_bag_and_the_equipment_as_they_are_until_the_server_syncs_them()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring);

        inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring1, Items);

        Assert.That(inventory.Bag[RingInBagSlot], Is.EqualTo(Ring));
        Assert.That(inventory.Equipment[(int)EquipmentSlot.Ring1], Is.Null);
    }

    [Test]
    public void An_item_equips_from_a_filled_slot_of_an_unlocked_bag_when_it_fits_a_link()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring);

        Assert.That(inventory.CanEquipBagItem(RingInBagSlot, Items), Is.True);
        Assert.That(inventory.CanEquipBagItem(5, Items), Is.False, "an empty slot");
        Assert.That(_inventory.CanEquipBagItem(1, Items), Is.False, "an item with no link");
        Assert.That(BuildLockedInventoryWith(RingInBagSlot, Ring).CanEquipBagItem(RingInBagSlot, Items), Is.False,
            "a locked bag");
    }

    [Test]
    public void Using_a_bag_item_sends_its_slot()
    {
        var inventory = BuildInventoryWith(4, Potion);

        var command = inventory.UseBagItem(4, Items, Now)!;

        Assert.That((command.WorldId, command.BagSlot), Is.EqualTo((PlayerId, 4)));
    }

    [Test]
    public void A_use_leaves_the_bag_as_it_is_until_the_server_syncs_it()
    {
        var inventory = BuildInventoryWith(4, Potion);

        inventory.UseBagItem(4, Items, Now);

        Assert.That(inventory.Bag[4], Is.EqualTo(Potion));
    }

    [TestCase(1_847, TestName = "an item whose first equip link is -1")]
    [TestCase(3_700, TestName = "an item with no equip link list")]
    public void An_item_with_a_use_script_and_no_equip_link_is_usable(int itemId)
    {
        Assert.That(CharacterInventory.IsUsable(Items[itemId]), Is.True);
    }

    [TestCase(3_000, TestName = "an item with no use script and no equip link")]
    [TestCase(4_200, TestName = "an item with an equip link and no use script")]
    [TestCase(3_500, TestName = "an item with an equip link and a use script")]
    [TestCase(3_600, TestName = "an item whose first equip link is not an equipment slot")]
    [TestCase(3_300, TestName = "a skill item")]
    [TestCase(3_400, TestName = "an item that asks for a target item")]
    [TestCase(865, TestName = "an item that asks for a target item by its id")]
    public void An_item_that_is_not_usable_sends_no_use(int itemId)
    {
        var inventory = BuildInventoryWith(4, BuildItem(itemId));

        Assert.That(CharacterInventory.IsUsable(Items[itemId]), Is.False);
        Assert.That(inventory.CanUseBagItem(4, Items, Now), Is.False);
        Assert.That(inventory.UseBagItem(4, Items, Now), Is.Null);
    }

    [TestCase(5, TestName = "from an empty slot")]
    [TestCase(24, TestName = "from a slot past the capacity")]
    [TestCase(-1, TestName = "from a slot before the bag")]
    public void A_use_the_bag_cannot_make_sends_nothing(int source)
    {
        var inventory = BuildInventoryWith(4, Potion);

        Assert.That(inventory.UseBagItem(source, Items, Now), Is.Null);
    }

    [Test]
    public void An_item_missing_from_the_item_table_sends_no_use()
    {
        var inventory = BuildInventoryWith(4, BuildItem(9_999));

        Assert.That(inventory.UseBagItem(4, Items, Now), Is.Null);
    }

    [Test]
    public void A_locked_bag_sends_no_use()
    {
        var inventory = BuildLockedInventoryWith(4, Potion);

        Assert.That(inventory.CanUseBagItem(4, Items, Now), Is.False);
        Assert.That(inventory.UseBagItem(4, Items, Now), Is.Null);
    }

    [Test]
    public void A_bag_that_waits_for_its_unlock_sends_no_use()
    {
        var inventory = BuildLockedInventoryWith(4, Potion);
        inventory.UnlockBag("123456");

        Assert.That(inventory.UseBagItem(4, Items, Now), Is.Null);
    }

    [Test]
    public void A_use_blocks_every_slot_with_the_same_item_until_the_cooldown_ends()
    {
        var inventory = BuildInventoryWith((4, Potion), (9, Potion), (10, Elixir));

        inventory.UseBagItem(4, Items, Now);

        Assert.That(inventory.CanUseBagItem(4, Items, Now.AddSeconds(0.5)), Is.False);
        Assert.That(inventory.UseBagItem(9, Items, Now.AddSeconds(0.5)), Is.Null);
        Assert.That(inventory.CanUseBagItem(10, Items, Now.AddSeconds(0.5)), Is.True, "another item");
        Assert.That(inventory.UseBagItem(9, Items, Now.AddSeconds(1)), Is.Not.Null, "the cooldown is 1 s");
    }

    [Test]
    public void The_cooldown_left_is_the_part_of_the_item_cooldown_that_is_still_to_run()
    {
        var inventory = BuildInventoryWith(10, Elixir);

        inventory.UseBagItem(10, Items, Now);

        Assert.That(inventory.CooldownLeftOf(Elixir.ItemId, Now), Is.EqualTo(1f));
        Assert.That(inventory.CooldownLeftOf(Elixir.ItemId, Now.AddSeconds(1)), Is.EqualTo(0.75f));
        Assert.That(inventory.CooldownLeftOf(Elixir.ItemId, Now.AddSeconds(4)), Is.Zero);
        Assert.That(inventory.CooldownLeftOf(Elixir.ItemId, Now.AddSeconds(9)), Is.Zero);
        Assert.That(inventory.CooldownLeftOf(Potion.ItemId, Now), Is.Zero, "an item that was not used");
    }

    [Test]
    public void An_item_with_no_cooldown_is_usable_again_at_once()
    {
        var inventory = BuildInventoryWith(4, Firework);

        inventory.UseBagItem(4, Items, Now);

        Assert.That(inventory.CooldownLeftOf(Firework.ItemId, Now), Is.Zero);
        Assert.That(inventory.UseBagItem(4, Items, Now), Is.Not.Null);
    }

    [Test]
    public void A_use_the_inventory_refuses_starts_no_cooldown()
    {
        var inventory = BuildLockedInventoryWith(4, Potion);

        inventory.UseBagItem(4, Items, Now);

        Assert.That(inventory.CooldownLeftOf(Potion.ItemId, Now), Is.Zero);
    }

    [Test]
    public void Unequipping_sends_the_equipment_slot_and_the_bag_slot()
    {
        var command = _inventory.UnequipItem(EquipmentSlot.Body, 5)!;

        Assert.That((command.WorldId, command.Slot, command.BagSlot), Is.EqualTo((PlayerId, EquipmentSlot.Body, 5)));
        Assert.That(_inventory.Equipment[(int)EquipmentSlot.Body], Is.EqualTo(Vest), "the server syncs the unequip");
    }

    [Test]
    public void An_item_unequips_from_a_filled_slot_of_an_unlocked_bag()
    {
        Assert.That(_inventory.CanUnequipItemFrom(EquipmentSlot.Body), Is.True);
        Assert.That(_inventory.CanUnequipItemFrom(EquipmentSlot.Head), Is.False, "an empty slot");

        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanUnequipItemFrom(EquipmentSlot.Body), Is.False, "a locked bag");
    }

    [TestCase(EquipmentSlot.Head, 5, TestName = "from an empty equipment slot")]
    [TestCase(EquipmentSlot.Body, 1, TestName = "onto a filled bag slot")]
    [TestCase(EquipmentSlot.Body, 24, TestName = "onto a slot past the capacity")]
    [TestCase(EquipmentSlot.Body, -1, TestName = "onto a slot before the bag")]
    public void An_unequip_the_bag_cannot_take_sends_nothing(EquipmentSlot slot, int destination)
    {
        Assert.That(_inventory.CanUnequipItem(slot, destination), Is.False);
        Assert.That(_inventory.UnequipItem(slot, destination), Is.Null);
    }

    [Test]
    public void A_locked_bag_sends_no_equip_and_no_unequip()
    {
        var inventory = BuildInventoryWith(RingInBagSlot, Ring, (EquipmentSlot.Body, Vest));
        inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(FittingSlots(inventory, RingInBagSlot), Is.Empty);
        Assert.That(inventory.EquipBagItem(RingInBagSlot, EquipmentSlot.Ring1, Items), Is.Null);
        Assert.That(inventory.EquipBagItem(RingInBagSlot, Items), Is.Null);
        Assert.That(inventory.CanUnequipItem(EquipmentSlot.Body, 5), Is.False);
        Assert.That(inventory.UnequipItem(EquipmentSlot.Body, 5), Is.Null);
    }

    [Test]
    public void A_bag_that_waits_for_its_unlock_sends_no_equip_and_no_unequip()
    {
        var inventory = new CharacterInventory(BuildMapEntered(new BagSync(24, [new BagSlot(RingInBagSlot, Ring)]),
            BuildEquipment((EquipmentSlot.Body, Vest)), isBagLocked: true));
        inventory.UnlockBag("123456");

        Assert.That(inventory.EquipBagItem(RingInBagSlot, Items), Is.Null);
        Assert.That(inventory.UnequipItem(EquipmentSlot.Body, 5), Is.Null);
    }

    [Test]
    public void Deleting_a_bag_item_sends_its_bag_slot()
    {
        var command = _inventory.DeleteBagItem(1, Items);

        Assert.That(command, Is.Not.Null);
        Assert.That((command!.WorldId, command.BagSlot), Is.EqualTo((PlayerId, 1)));
    }

    [Test]
    public void A_delete_leaves_the_bag_as_it_is_until_the_server_syncs_it()
    {
        _inventory.DeleteBagItem(1, Items);

        Assert.That(_inventory.Bag.Take(2), Is.EqualTo(new[] { Sword, Apple }));
    }

    [Test]
    public void An_item_deletes_from_a_filled_slot_inside_the_capacity()
    {
        var inventory = new CharacterInventory(BuildMapEntered(
            new BagSync(24, [new BagSlot(1, Apple), new BagSlot(30, Apple)]), BuildEquipment()));

        Assert.That(inventory.CanDeleteBagItem(1, Items), Is.True);
        Assert.That(inventory.CanDeleteBagItem(2, Items), Is.False, "an empty slot");
        Assert.That(inventory.CanDeleteBagItem(30, Items), Is.False, "a slot outside the capacity");
        Assert.That(inventory.DeleteBagItem(30, Items), Is.Null);
    }

    [Test]
    public void An_item_the_server_does_not_delete_sends_no_delete()
    {
        var inventory = new CharacterInventory(BuildMapEntered(new BagSync(24,
        [
            new BagSlot(0, Map), new BagSlot(1, Apple with { IsLocked = true }), new BagSlot(2, BuildItem(9_999)),
        ]), BuildEquipment()));

        Assert.That(inventory.CanDeleteBagItem(0, Items), Is.False, "the item table marks it not deletable");
        Assert.That(inventory.CanDeleteBagItem(1, Items), Is.False, "the item is locked");
        Assert.That(inventory.CanDeleteBagItem(2, Items), Is.False, "the item is not in the item table");
        Assert.That(inventory.DeleteBagItem(0, Items), Is.Null);
    }

    [Test]
    public void Throwing_a_bag_item_sends_its_bag_slot_the_count_and_the_drop_position()
    {
        var command = _inventory.ThrowBagItem(1, 7, new Vector2(2134f, 2786f), Items);

        Assert.That(command, Is.Not.Null);
        Assert.That((command!.WorldId, command.BagSlot, command.Count, command.DropPosition),
            Is.EqualTo((PlayerId, 1, 7, new Vector2(2134f, 2786f))));
    }

    [TestCase(0)]
    [TestCase(21)]
    public void A_throw_of_no_item_or_of_more_than_the_stack_sends_nothing(int count)
    {
        Assert.That(_inventory.ThrowBagItem(1, count, Vector2.Zero, Items), Is.Null);
    }

    [Test]
    public void A_throw_leaves_the_bag_as_it_is_until_the_server_syncs_it()
    {
        _inventory.ThrowBagItem(1, 20, Vector2.Zero, Items);

        Assert.That(_inventory.Bag.Take(2), Is.EqualTo(new[] { Sword, Apple }));
    }

    [Test]
    public void An_item_the_server_does_not_throw_sends_no_throw()
    {
        var inventory = new CharacterInventory(BuildMapEntered(new BagSync(24,
        [
            new BagSlot(0, Map), new BagSlot(1, Apple with { IsLocked = true }),
            new BagSlot(2, Apple with { IsTradable = false }), new BagSlot(3, Apple), new BagSlot(30, Apple),
        ]), BuildEquipment()));

        Assert.That(inventory.CanThrowBagItem(0, Items), Is.False, "the item table marks it not throwable");
        Assert.That(inventory.CanThrowBagItem(1, Items), Is.False, "the item is locked");
        Assert.That(inventory.CanThrowBagItem(2, Items), Is.False, "the item is not tradable");
        Assert.That(inventory.CanThrowBagItem(3, Items), Is.True);
        Assert.That(inventory.CanThrowBagItem(4, Items), Is.False, "an empty slot");
        Assert.That(inventory.CanThrowBagItem(30, Items), Is.False, "a slot outside the capacity");
        Assert.That(inventory.ThrowBagItem(0, 1, Vector2.Zero, Items), Is.Null);
    }

    [Test]
    public void A_locked_bag_deletes_and_throws_nothing()
    {
        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanDeleteBagItem(1, Items), Is.False);
        Assert.That(_inventory.DeleteBagItem(1, Items), Is.Null);
        Assert.That(_inventory.CanThrowBagItem(1, Items), Is.False);
        Assert.That(_inventory.ThrowBagItem(1, 1, Vector2.Zero, Items), Is.Null);
    }

    [Test]
    public void A_bag_that_waits_for_its_unlock_deletes_and_throws_nothing()
    {
        var inventory = new CharacterInventory(
            BuildMapEntered(new BagSync(24, [new BagSlot(1, Apple)]), BuildEquipment(), isBagLocked: true));
        inventory.UnlockBag("123456");

        Assert.That(inventory.DeleteBagItem(1, Items), Is.Null);
        Assert.That(inventory.ThrowBagItem(1, 1, Vector2.Zero, Items), Is.Null);
    }

    [Test]
    public void Taking_a_temporary_bag_item_sends_its_whole_stack_to_the_bag_slot()
    {
        FillTemporaryBag((3, Rum));

        var command = _inventory.TakeTemporaryBagItem(3, 8, Items)!;

        Assert.That((command.WorldId, command.TemporarySlot, command.Count, command.BagSlot),
            Is.EqualTo((PlayerId, 3, 5, (int?)8)));
    }

    [TestCase(4, 8, TestName = "from an empty temporary slot")]
    [TestCase(16, 8, TestName = "from a slot past the temporary bag")]
    [TestCase(-1, 8, TestName = "from a slot before the temporary bag")]
    [TestCase(3, 24, TestName = "onto a slot past the capacity")]
    [TestCase(3, -1, TestName = "onto a slot before the bag")]
    [TestCase(3, 0, TestName = "onto another item")]
    public void A_take_the_bag_cannot_make_sends_nothing(int source, int destination)
    {
        FillTemporaryBag((3, Rum));

        Assert.That(_inventory.CanTakeTemporaryBagItem(source, destination, Items), Is.False);
        Assert.That(_inventory.TakeTemporaryBagItem(source, destination, Items), Is.Null);
    }

    [Test]
    public void A_temporary_bag_item_goes_into_an_empty_slot_or_onto_a_stack_of_the_same_item()
    {
        FillTemporaryBag((3, Rum), (4, Sword));
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(5, Rum)])));

        Assert.That(_inventory.CanTakeTemporaryBagItem(3, 8, Items), Is.True, "an empty slot");
        Assert.That(_inventory.CanTakeTemporaryBagItem(3, 5, Items), Is.True, "a stack of the same item");
        Assert.That(_inventory.CanTakeTemporaryBagItem(4, 0, Items), Is.False, "the same item that does not stack");
    }

    [Test]
    public void A_temporary_bag_item_goes_onto_a_stack_only_where_the_server_merges_the_two()
    {
        FillTemporaryBag((3, Rum), (4, Rum with { Expiration = 1_800_000_000 }));
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [
            new BagSlot(5, Rum with { Count = 99 }),
            new BagSlot(6, Rum with { IsTradable = false }),
            new BagSlot(7, Rum with { Expiration = 1_800_000_000 }),
            new BagSlot(8, Rum),
        ])));

        Assert.That(_inventory.CanTakeTemporaryBagItem(3, 5, Items), Is.False, "a full stack");
        Assert.That(_inventory.CanTakeTemporaryBagItem(3, 6, Items), Is.False, "a stack that trades differently");
        Assert.That(_inventory.CanTakeTemporaryBagItem(3, 7, Items), Is.False, "a timed stack");
        Assert.That(_inventory.CanTakeTemporaryBagItem(4, 8, Items), Is.False, "a timed item");
    }

    [Test]
    public void Splitting_a_temporary_bag_stack_sends_the_part_to_take_and_leaves_the_rest_in_its_slot()
    {
        FillTemporaryBag((3, Rum));

        var command = _inventory.SplitTemporaryBagItem(3, 2, 8, Items)!;

        Assert.That((command.WorldId, command.TemporarySlot, command.Count, command.BagSlot),
            Is.EqualTo((PlayerId, 3, 2, (int?)8)));
        Assert.That(_inventory.TemporaryBag[3], Is.EqualTo(Rum), "the server syncs the split");
    }

    [Test]
    public void A_temporary_bag_stack_splits_into_an_empty_slot_or_onto_a_stack_it_merges_with()
    {
        FillTemporaryBag((3, Rum), (4, Sword));
        _inventory.ChangeBag(new BagChangedEvent(PlayerId, new BagSync(null, [new BagSlot(5, Rum)])));

        Assert.That(_inventory.CanSplitTemporaryBagItem(3, 8, Items), Is.True, "an empty slot");
        Assert.That(_inventory.CanSplitTemporaryBagItem(3, 5, Items), Is.True, "a stack of the same item");
        Assert.That(_inventory.CanSplitTemporaryBagItem(3, 0, Items), Is.False, "another item");
        Assert.That(_inventory.CanSplitTemporaryBagItem(4, 8, Items), Is.False, "a single item");
        Assert.That(_inventory.SplitTemporaryBagItem(3, 2, 0, Items), Is.Null);
        Assert.That(_inventory.SplitTemporaryBagItem(4, 1, 8, Items), Is.Null);
    }

    [TestCase(0)]
    [TestCase(5)]
    [TestCase(6)]
    public void A_temporary_bag_split_that_leaves_no_part_on_either_side_sends_nothing(int count)
    {
        FillTemporaryBag((3, Rum));

        Assert.That(_inventory.SplitTemporaryBagItem(3, count, 8, Items), Is.Null);
    }

    [Test]
    public void A_locked_bag_splits_nothing_from_the_temporary_bag()
    {
        FillTemporaryBag((3, Rum));
        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanSplitTemporaryBagItem(3, 8, Items), Is.False);
        Assert.That(_inventory.SplitTemporaryBagItem(3, 2, 8, Items), Is.Null);
    }

    [Test]
    public void Taking_all_sends_each_temporary_bag_stack_for_the_server_to_put_in_the_bag()
    {
        FillTemporaryBag((0, Rum), (3, Apple), (9, Sword));

        var commands = _inventory.TakeAllTemporaryBagItems();

        Assert.That(commands.Select(command =>
                (command.WorldId, command.TemporarySlot, command.Count, command.BagSlot)),
            Is.EqualTo(new (uint, int, int, int?)[] { (PlayerId, 0, 5, null), (PlayerId, 3, 20, null), (PlayerId, 9, 1, null) }));
    }

    [Test]
    public void Take_all_needs_an_item_in_the_temporary_bag()
    {
        Assert.That(_inventory.CanTakeAllTemporaryBagItems, Is.False);
        Assert.That(_inventory.TakeAllTemporaryBagItems(), Is.Empty);

        FillTemporaryBag((9, Sword));

        Assert.That(_inventory.CanTakeAllTemporaryBagItems, Is.True);
    }

    [Test]
    public void A_locked_bag_takes_nothing_from_the_temporary_bag()
    {
        FillTemporaryBag((3, Rum));
        _inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(_inventory.CanTakeTemporaryBagItemFrom(3), Is.False);
        Assert.That(_inventory.TakeTemporaryBagItem(3, 8, Items), Is.Null);
        Assert.That(_inventory.CanTakeAllTemporaryBagItems, Is.False);
        Assert.That(_inventory.TakeAllTemporaryBagItems(), Is.Empty);
    }

    [Test]
    public void A_take_leaves_both_bags_as_they_are_until_the_server_syncs_them()
    {
        FillTemporaryBag((3, Rum));

        _inventory.TakeTemporaryBagItem(3, 8, Items);
        _inventory.TakeAllTemporaryBagItems();

        Assert.That(_inventory.TemporaryBag[3], Is.EqualTo(Rum));
        Assert.That(_inventory.Bag[8], Is.Null);
    }

    [Test]
    public void A_sort_merges_stacks_up_to_the_stack_limit_and_keeps_apart_the_stacks_the_server_does_not_merge()
    {
        var inventory = new CharacterInventory(BuildMapEntered(new BagSync(24,
        [
            new BagSlot(0, Rum with { Count = 60 }), new BagSlot(1, Rum with { Count = 70 }),
            new BagSlot(2, Rum with { Count = 50 }), new BagSlot(3, Rum with { Count = 4, Expiration = 1_900_000_000 }),
            new BagSlot(4, Rum with { Count = 3, IsTradable = false }),
        ]), BuildEquipment()));

        var moves = RunBagSort(inventory, BagSortKey.Name);

        Assert.That(moves, Is.EqualTo(new[] { (2, 50, 0), (2, 11, 1), (3, 4, 2), (4, 3, 3) }));
        Assert.That(BagContents(inventory).Take(5), Is.EqualTo(new (int, int)?[]
        {
            (Rum.ItemId, 99), (Rum.ItemId, 81), (Rum.ItemId, 4), (Rum.ItemId, 3), null,
        }));
    }

    [Test]
    public void A_sort_sends_one_move_and_leaves_the_bag_as_it_is_until_the_server_syncs_it()
    {
        var inventory = BuildUnsortedInventory();
        var before = BagContents(inventory);

        var first = inventory.SortBag(BagSortKey.Name, Items)!;

        Assert.That((first.SourceSlot, first.Count, first.DestinationSlot), Is.EqualTo((4, 30, 0)));
        Assert.That(BagContents(inventory), Is.EqualTo(before));
        Assert.That(inventory.IsSortingBag, Is.True);

        var second = inventory.ChangeBag(new BagChangedEvent(PlayerId, ServerMove(inventory.Bag, first)))!;

        Assert.That((second.SourceSlot, second.Count, second.DestinationSlot), Is.EqualTo((7, 20, 0)));
        Assert.That(BagContents(inventory)[0], Is.EqualTo((Rum.ItemId, 35)));
    }

    [Test]
    public void A_sort_ends_once_the_server_syncs_its_last_move()
    {
        var inventory = BuildUnsortedInventory();

        RunBagSort(inventory, BagSortKey.Name);

        Assert.That(inventory.IsSortingBag, Is.False);
        Assert.That(inventory.CanSortBag, Is.True);
    }

    [Test]
    public void A_sorted_bag_sends_no_move()
    {
        var inventory = BuildUnsortedInventory();

        RunBagSort(inventory, BagSortKey.Type);

        Assert.That(inventory.SortBag(BagSortKey.Type, Items), Is.Null);
        Assert.That(inventory.IsSortingBag, Is.False);
    }

    [Test]
    public void A_locked_bag_sorts_nothing()
    {
        var inventory = BuildUnsortedInventory();

        inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(inventory.CanSortBag, Is.False);
        Assert.That(inventory.SortBag(BagSortKey.Name, Items), Is.Null);
        Assert.That(inventory.IsSortingBag, Is.False);
    }

    [Test]
    public void A_second_sort_waits_until_the_first_one_ends()
    {
        var inventory = BuildUnsortedInventory();

        inventory.SortBag(BagSortKey.Name, Items);

        Assert.That(inventory.CanSortBag, Is.False);
        Assert.That(inventory.SortBag(BagSortKey.Type, Items), Is.Null);
    }

    [Test]
    public void A_sort_stops_at_the_move_the_server_refuses_and_the_bag_keeps_what_the_server_synced()
    {
        var inventory = BuildUnsortedInventory();
        var first = inventory.SortBag(BagSortKey.Name, Items)!;

        inventory.ChangeBag(new BagChangedEvent(PlayerId, ServerMove(inventory.Bag, first)));
        inventory.FailItemAction();

        Assert.That(inventory.IsSortingBag, Is.False);
        Assert.That(BagContents(inventory).Take(2), Is.EqualTo(new (int, int)?[] { (Rum.ItemId, 35), (Sword.ItemId, 1) }));
    }

    [Test]
    public void A_sort_stops_when_the_bag_locks()
    {
        var inventory = BuildUnsortedInventory();

        inventory.SortBag(BagSortKey.Name, Items);
        inventory.ChangeBagLock(new BagLockReportedEvent(true));

        Assert.That(inventory.IsSortingBag, Is.False);
    }

    [Test]
    public void A_sort_stops_when_the_server_syncs_a_bag_it_did_not_plan()
    {
        var inventory = BuildUnsortedInventory();

        inventory.SortBag(BagSortKey.Name, Items);
        var next = inventory.ChangeBag(new BagChangedEvent(PlayerId,
            new BagSync(null, [new BagSlot(4, null), new BagSlot(2, Rum with { Count = 30 })])));

        Assert.That(next, Is.Null);
        Assert.That(inventory.IsSortingBag, Is.False);
        Assert.That(inventory.Bag[2], Is.EqualTo(Rum with { Count = 30 }));
    }

    [Test]
    public void A_bag_that_sorts_moves_takes_equips_unequips_deletes_and_throws_nothing_else_until_the_sort_ends()
    {
        var inventory = BuildUnsortedInventory();

        inventory.ChangeTemporaryBag(new TemporaryBagChangedEvent(new BagSync(null, [new BagSlot(0, Apple)])));
        inventory.ChangeEquipment(BuildLookChanged(PlayerId, (EquipmentSlot.Body, Vest)));
        inventory.SortBag(BagSortKey.Name, Items);

        Assert.That(inventory.MoveBagItem(1, 2), Is.Null);
        Assert.That(inventory.EquipBagItem(1, Items), Is.Null);
        Assert.That(inventory.UnequipItem(EquipmentSlot.Body, 2), Is.Null);
        Assert.That(inventory.TakeTemporaryBagItem(0, 2, Items), Is.Null);
        Assert.That(inventory.TakeAllTemporaryBagItems(), Is.Empty);
        Assert.That(inventory.DeleteBagItem(1, Items), Is.Null);
        Assert.That(inventory.ThrowBagItem(1, 1, Vector2.Zero, Items), Is.Null);

        inventory.FailItemAction();

        Assert.That(inventory.MoveBagItem(1, 2), Is.Not.Null);
    }

    [Test]
    public void Sorting_by_type_puts_equipment_then_usable_items_then_materials_then_quest_items_each_by_item_type()
    {
        var inventory = BuildUnsortedInventory();

        var moves = RunBagSort(inventory, BagSortKey.Type);

        Assert.That(moves, Is.EqualTo(new[] { (4, 30, 0), (1, 1, 0), (8, 1, 1), (5, 1, 2), (8, 35, 3), (7, 20, 4), (8, 1, 5) }));
        Assert.That(BagContents(inventory), Is.EqualTo(new (int, int)?[]
        {
            (Sword.ItemId, 1), (Vest.ItemId, 1), (Ring.ItemId, 1), (Rum.ItemId, 35), (Apple.ItemId, 20),
            (Map.ItemId, 1), null, null, null,
        }));
    }

    [Test]
    public void Sorting_by_rarity_puts_the_rarest_equipment_first_and_items_with_no_rarity_last_by_type()
    {
        var inventory = BuildUnsortedInventory();

        var moves = RunBagSort(inventory, BagSortKey.Rarity);

        Assert.That(moves, Is.EqualTo(new[] { (4, 30, 0), (5, 1, 0), (8, 1, 1), (8, 1, 2), (5, 35, 3), (7, 20, 4) }));
        Assert.That(BagContents(inventory), Is.EqualTo(new (int, int)?[]
        {
            (Ring.ItemId, 1), (Vest.ItemId, 1), (Sword.ItemId, 1), (Rum.ItemId, 35), (Apple.ItemId, 20),
            (Map.ItemId, 1), null, null, null,
        }));
    }

    [Test]
    public void Sorting_by_name_merges_the_stacks_then_moves_each_item_into_its_place_in_order_of_name()
    {
        var inventory = BuildUnsortedInventory();

        var moves = RunBagSort(inventory, BagSortKey.Name);

        Assert.That(moves, Is.EqualTo(new[] { (4, 30, 0), (7, 20, 0), (5, 1, 1), (7, 35, 2), (8, 1, 3), (5, 1, 4), (8, 1, 5) }));
        Assert.That(BagContents(inventory), Is.EqualTo(new (int, int)?[]
        {
            (Apple.ItemId, 20), (Ring.ItemId, 1), (Rum.ItemId, 35), (Vest.ItemId, 1), (Sword.ItemId, 1),
            (Map.ItemId, 1), null, null, null,
        }));
    }

    private static ItemEntry EntryOf(BagItem item)
    {
        Items.TryGetById(item.ItemId, out var entry);

        return entry;
    }

    private static IEnumerable<EquipmentSlot> FittingSlots(CharacterInventory inventory, int bagSlot)
    {
        return Enum.GetValues<EquipmentSlot>().Where(slot => inventory.CanEquipBagItem(bagSlot, slot, Items));
    }

    private static CharacterInventory BuildUnsortedInventory()
    {
        return new CharacterInventory(BuildMapEntered(new BagSync(24,
        [
            new BagSlot(0, Rum), new BagSlot(1, Sword), new BagSlot(3, Map), new BagSlot(4, Rum with { Count = 30 }),
            new BagSlot(5, BuildItem(Ring.ItemId, rarity: 5)), new BagSlot(7, Apple),
            new BagSlot(8, BuildItem(Vest.ItemId, rarity: 3)),
        ]), BuildEquipment()));
    }

    private static List<(int Source, int Count, int Destination)> RunBagSort(CharacterInventory inventory,
        BagSortKey key)
    {
        var moves = new List<(int, int, int)>();
        var command = inventory.SortBag(key, Items);

        while (command != null && moves.Count < CharacterInventory.BagSlotCount * 2)
        {
            moves.Add((command.SourceSlot, command.Count, command.DestinationSlot));
            command = inventory.ChangeBag(new BagChangedEvent(PlayerId, ServerMove(inventory.Bag, command)));
        }

        return moves;
    }

    private static BagSync ServerMove(IReadOnlyList<BagItem?> bag, MoveBagItemCommand move)
    {
        var source = bag[move.SourceSlot]!;
        var target = bag[move.DestinationSlot];
        var stackLimit = Items.TryGetById(source.ItemId, out var item) ? item.StackLimit : 0;

        if (target != null && target.ItemId == source.ItemId && target.Count < stackLimit)
        {
            var moved = Math.Min(move.Count, stackLimit - target.Count);

            return new BagSync(null,
            [
                new BagSlot(move.SourceSlot, source.Count > moved ? source with { Count = source.Count - moved } : null),
                new BagSlot(move.DestinationSlot, target with { Count = target.Count + moved }),
            ]);
        }

        return new BagSync(null, [new BagSlot(move.SourceSlot, target), new BagSlot(move.DestinationSlot, source)]);
    }

    private static (int ItemId, int Count)?[] BagContents(CharacterInventory inventory)
    {
        return inventory.Bag.Take(9).Select(item => item is { } held ? (held.ItemId, held.Count) : ((int, int)?)null)
            .ToArray();
    }

    private void FillTemporaryBag(params (int Slot, BagItem Item)[] items)
    {
        _inventory.ChangeTemporaryBag(new TemporaryBagChangedEvent(
            new BagSync(null, items.Select(item => new BagSlot(item.Slot, item.Item)).ToList())));
    }

    private static CharacterInventory BuildInventoryWith(int bagSlot, BagItem item,
        params (EquipmentSlot Slot, BagItem Item)[] equipment)
    {
        return new CharacterInventory(
            BuildMapEntered(new BagSync(24, [new BagSlot(bagSlot, item)]), BuildEquipment(equipment)));
    }

    private static CharacterInventory BuildInventoryWith(params (int Slot, BagItem Item)[] items)
    {
        return new CharacterInventory(BuildMapEntered(
            new BagSync(24, items.Select(item => new BagSlot(item.Slot, item.Item)).ToList()), BuildEquipment()));
    }

    private static CharacterInventory BuildLockedInventoryWith(int bagSlot, BagItem item)
    {
        return new CharacterInventory(BuildMapEntered(new BagSync(24, [new BagSlot(bagSlot, item)]),
            BuildEquipment(), isBagLocked: true));
    }

    private static CharacterInventory BuildLockedInventory()
    {
        return new CharacterInventory(BuildMapEntered(new BagSync(24, []), BuildEquipment(), isBagLocked: true));
    }

    private static BagItem BuildItem(int itemId, int count = 1, int rarity = 0)
    {
        return new BagItem(itemId, 0, count, 0, 0, 0, rarity * 1_000, 0, true, true, false, 0, 0, 0,
            new Dictionary<CharacterStat, int>());
    }

    private static BagItem?[] BuildEquipment(params (EquipmentSlot Slot, BagItem Item)[] items)
    {
        var equipment = new BagItem?[34];

        foreach (var (slot, item) in items)
        {
            equipment[(int)slot] = item;
        }

        return equipment;
    }

    private static CharacterLookChangedEvent BuildLookChanged(uint worldId,
        params (EquipmentSlot Slot, BagItem Item)[] items)
    {
        return new CharacterLookChangedEvent(worldId, new CharacterLook(0, new int[34]), BuildEquipment(items));
    }

    private static MapEnteredEvent BuildMapEntered(BagSync bag, BagItem?[] equipment, bool isBagLocked = false)
    {
        var player = new CharacterState(PlayerId, CharacterControlType.Player, 1, "T1", Vector2.Zero, 0, 0, 0, 0,
            string.Empty, string.Empty, 0, 1, 100, 100, 20, 20, 0, 0, 0, 5, 2_500, 0f, null, null);

        return new MapEnteredEvent("garner", EnterType.Carry, player, bag, isBagLocked, equipment);
    }
}
