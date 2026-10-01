using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Top.Client.Features.Inventory.Bag;
using Top.Client.Features.Inventory.Tooltip;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Play.Events;
using Top.Contracts.Tables.Items;
using Top.Logging;

namespace Top.Client.Features.Inventory;

public class CharacterInventory
{
    public const int BagSlotCount = 48;

    // TODO: the server holds 32 temporary-bag slots; slots from 16 up are dropped (known issue).
    public const int TemporaryBagSlotCount = 16;

    public const int EquipmentSlotCount = 34;

    private const int SwordItemType = 1;

    private const int RingItemType = 26;

    private const int QuestItemType = 42;

    private const int TargetItemType = 66;

    private const int SkillItemType = 71;

    private const int FirstTargetItemId = 864;

    private const int LastTargetItemId = 866;

    private const int NoEquipLink = -1;

    private readonly BagItem?[] _bag = new BagItem?[BagSlotCount];

    private readonly Dictionary<int, ItemCooldown> _cooldowns = [];

    private readonly BagItem?[] _temporaryBag = new BagItem?[TemporaryBagSlotCount];

    private readonly BagItem?[] _equipment = new BagItem?[EquipmentSlotCount];

    private readonly Queue<BagSortStep> _bagSortSteps = new();

    private readonly uint _playerId;

    public CharacterInventory(MapEnteredEvent entered)
    {
        _playerId = entered.Character.WorldId;

        ApplyBag(entered.Bag);
        ApplyEquipment(entered.Equipment);
        IsBagLocked = entered.IsBagLocked;
    }

    public IReadOnlyList<BagItem?> Bag => _bag;

    public int BagCapacity { get; private set; }

    public IReadOnlyList<BagItem?> TemporaryBag => _temporaryBag;

    public IReadOnlyList<BagItem?> Equipment => _equipment;

    public bool IsBagLocked { get; private set; }

    public bool IsUnlockingBag { get; private set; }

    public bool IsSortingBag => _bagSortSteps.Count > 0;

    public bool CanSortBag => CanChangeBag;

    public bool CanTakeAllTemporaryBagItems => TakeableTemporaryBagSlots.Any();

    public MoveBagItemCommand? ChangeBag(BagChangedEvent changed)
    {
        if (changed.WorldId != _playerId)
        {
            return null;
        }

        ApplyBag(changed.Sync);

        if (!_bagSortSteps.TryDequeue(out var sent))
        {
            return null;
        }

        if (!HoldsBag(sent.Bag))
        {
            _bagSortSteps.Clear();

            return null;
        }

        return _bagSortSteps.TryPeek(out var next) ? next.Move : null;
    }

    public void ChangeTemporaryBag(TemporaryBagChangedEvent changed)
    {
        ApplySlots(_temporaryBag, changed.Sync.Slots, "temporary bag");
    }

    public void ChangeEquipment(CharacterLookChangedEvent changed)
    {
        if (changed.WorldId == _playerId)
        {
            ApplyEquipment(changed.Equipment);
        }
    }

    public bool CanMoveBagItemFrom(int source)
    {
        return CanChangeBag && IsUsableBagSlot(source) && _bag[source] != null;
    }

    public bool CanMoveBagItem(int source, int destination)
    {
        return CanMoveBagItemFrom(source) && source != destination && IsUsableBagSlot(destination);
    }

    public MoveBagItemCommand? MoveBagItem(int source, int destination)
    {
        return CanMoveBagItem(source, destination)
            ? new MoveBagItemCommand(_playerId, source, _bag[source]!.Count, destination)
            : null;
    }

    public bool CanSplitBagItem(int source, int destination)
    {
        return CanMoveBagItem(source, destination) && _bag[source] is { Count: > 1 } stack
            && (_bag[destination] is not { } target || target.ItemId == stack.ItemId);
    }

    public MoveBagItemCommand? SplitBagItem(int source, int count, int destination)
    {
        return CanSplitBagItem(source, destination) && count >= 1 && count < _bag[source]!.Count
            ? new MoveBagItemCommand(_playerId, source, count, destination)
            : null;
    }

    public bool CanDeleteBagItem(int source, ItemTable items)
    {
        return AllowsBagItem(source, ItemRestrictions.Delete, items);
    }

    public DeleteBagItemCommand? DeleteBagItem(int source, ItemTable items)
    {
        return CanDeleteBagItem(source, items) ? new DeleteBagItemCommand(_playerId, source) : null;
    }

    public bool CanThrowBagItem(int source, ItemTable items)
    {
        return AllowsBagItem(source, ItemRestrictions.Throw, items);
    }

    public ThrowBagItemCommand? ThrowBagItem(int source, int count, Vector2 dropPosition, ItemTable items)
    {
        return CanThrowBagItem(source, items) && count >= 1 && count <= _bag[source]!.Count
            ? new ThrowBagItemCommand(_playerId, source, count, dropPosition)
            : null;
    }

    public bool CanTakeTemporaryBagItemFrom(int source)
    {
        return CanChangeBag && IsTemporaryBagSlot(source) && _temporaryBag[source] != null;
    }

    public bool CanTakeTemporaryBagItem(int source, int destination, ItemTable items)
    {
        return CanTakeTemporaryBagItemFrom(source) && IsUsableBagSlot(destination)
            && (_bag[destination] is not { } target || StacksOnto(_temporaryBag[source]!, target, items));
    }

    public TakeTemporaryBagItemCommand? TakeTemporaryBagItem(int source, int destination, ItemTable items)
    {
        return CanTakeTemporaryBagItem(source, destination, items)
            ? new TakeTemporaryBagItemCommand(_playerId, source, _temporaryBag[source]!.Count, destination)
            : null;
    }

    public bool CanSplitTemporaryBagItem(int source, int destination, ItemTable items)
    {
        return CanTakeTemporaryBagItem(source, destination, items) && _temporaryBag[source] is { Count: > 1 };
    }

    public TakeTemporaryBagItemCommand? SplitTemporaryBagItem(int source, int count, int destination,
        ItemTable items)
    {
        return CanSplitTemporaryBagItem(source, destination, items) && count >= 1
            && count < _temporaryBag[source]!.Count
                ? new TakeTemporaryBagItemCommand(_playerId, source, count, destination)
                : null;
    }

    public IReadOnlyList<TakeTemporaryBagItemCommand> TakeAllTemporaryBagItems()
    {
        return TakeableTemporaryBagSlots
            .Select(source => new TakeTemporaryBagItemCommand(_playerId, source, _temporaryBag[source]!.Count, null))
            .ToList();
    }

    public MoveBagItemCommand? SortBag(BagSortKey key, ItemTable items)
    {
        if (!CanSortBag)
        {
            return null;
        }

        var bag = _bag.Take(Math.Min(BagCapacity, BagSlotCount)).ToArray();
        var comparer = CreateBagSortComparer(key, items);

        MergeStacks(bag, items);
        OrderItems(bag, comparer, items);

        return _bagSortSteps.TryPeek(out var first) ? first.Move : null;
    }

    public void FailItemAction()
    {
        _bagSortSteps.Clear();
    }

    public bool CanEquipBagItem(int source, EquipmentSlot slot, ItemTable items)
    {
        return FindEquipmentItem(source, items) is { } item
            && (slot == DefaultSlotOf(item) || slot == SecondSlotOf(item));
    }

    public EquipBagItemCommand? EquipBagItem(int source, EquipmentSlot slot, ItemTable items)
    {
        if (FindEquipmentItem(source, items) is not { } item)
        {
            return null;
        }

        var isDefaultSlot = slot == DefaultSlotOf(item);

        return isDefaultSlot || slot == SecondSlotOf(item)
            ? new EquipBagItemCommand(_playerId, source, !isDefaultSlot)
            : null;
    }

    public bool CanEquipBagItem(int source, ItemTable items)
    {
        return FindEquipmentItem(source, items) != null;
    }

    public EquipBagItemCommand? EquipBagItem(int source, ItemTable items)
    {
        return CanEquipBagItem(source, items) ? new EquipBagItemCommand(_playerId, source, false) : null;
    }

    public static bool IsUsable(ItemEntry item)
    {
        return item.ScriptEffect != null && (item.EquipLinks?.FirstOrDefault() ?? NoEquipLink) == NoEquipLink
            && item.Type is not (SkillItemType or TargetItemType)
            && item.Id is not (>= FirstTargetItemId and <= LastTargetItemId);
    }

    public bool CanUseBagItem(int source, ItemTable items, DateTime now)
    {
        return FindUsableItem(source, items) is { } item && CooldownLeftOf(item.Id, now) == 0f;
    }

    public UseBagItemCommand? UseBagItem(int source, ItemTable items, DateTime now)
    {
        if (FindUsableItem(source, items) is not { } item || CooldownLeftOf(item.Id, now) > 0f)
        {
            return null;
        }

        if (item.Cooldown > 0f)
        {
            _cooldowns[item.Id] = new ItemCooldown(now, TimeSpan.FromSeconds(item.Cooldown));
        }

        return new UseBagItemCommand(_playerId, source);
    }

    public float CooldownLeftOf(int itemId, DateTime now)
    {
        if (!_cooldowns.TryGetValue(itemId, out var cooldown))
        {
            return 0f;
        }

        var left = cooldown.StartedAt + cooldown.Length - now;

        return Math.Clamp((float)(left / cooldown.Length), 0f, 1f);
    }

    public bool CanUnequipItemFrom(EquipmentSlot slot)
    {
        return CanChangeBag && _equipment[(int)slot] != null;
    }

    public bool CanUnequipItem(EquipmentSlot slot, int destination)
    {
        return CanUnequipItemFrom(slot) && IsUsableBagSlot(destination) && _bag[destination] == null;
    }

    public UnequipItemCommand? UnequipItem(EquipmentSlot slot, int destination)
    {
        return CanUnequipItem(slot, destination) ? new UnequipItemCommand(_playerId, slot, destination) : null;
    }

    public static IReadOnlyList<EquipmentSlot> FittingSlotsOf(ItemEntry item)
    {
        return (item.EquipLinks ?? [])
            .TakeWhile(link => link is >= 0 and < EquipmentSlotCount)
            .Select(link => (EquipmentSlot)link)
            .ToList();
    }

    public IReadOnlyList<EquipmentSlot> FilledFittingSlotsOf(ItemEntry item)
    {
        return FittingSlotsOf(item).Where(slot => _equipment[(int)slot] != null).ToList();
    }

    public LockBagCommand LockBag()
    {
        return new LockBagCommand();
    }

    public UnlockBagCommand UnlockBag(string code)
    {
        var command = new UnlockBagCommand(code);

        IsUnlockingBag = true;

        return command;
    }

    public BagLockResult ChangeBagLock(BagLockReportedEvent reported)
    {
        var wasUnlocking = IsUnlockingBag;

        IsBagLocked = reported.IsLocked;
        IsUnlockingBag = false;

        if (!reported.IsLocked)
        {
            return BagLockResult.Unlocked;
        }

        _bagSortSteps.Clear();

        return wasUnlocking ? BagLockResult.CodeRejected : BagLockResult.Locked;
    }

    private bool CanChangeBag => !IsBagLocked && !IsSortingBag;

    private IEnumerable<int> TakeableTemporaryBagSlots =>
        Enumerable.Range(0, TemporaryBagSlotCount).Where(CanTakeTemporaryBagItemFrom);

    private bool IsUsableBagSlot(int slot)
    {
        return slot >= 0 && slot < BagCapacity && slot < BagSlotCount;
    }

    private bool AllowsBagItem(int source, ItemRestrictions action, ItemTable items)
    {
        return CanMoveBagItemFrom(source) && items.TryGetById(_bag[source]!.ItemId, out var item)
            && !ItemDescriber.RestrictionsOf(item, _bag[source]!).HasFlag(action);
    }

    private static bool IsTemporaryBagSlot(int slot)
    {
        return slot >= 0 && slot < TemporaryBagSlotCount;
    }

    private static bool StacksOnto(BagItem source, BagItem target, ItemTable items)
    {
        return source.ItemId == target.ItemId && items.TryGetById(target.ItemId, out var item)
            && target.Count < item.StackLimit && source.Expiration == 0 && target.Expiration == 0
            && source.IsTradable == target.IsTradable;
    }

    private void MergeStacks(BagItem?[] bag, ItemTable items)
    {
        for (var target = 0; target < bag.Length; target++)
        {
            for (var source = bag.Length - 1; source > target; source--)
            {
                if (bag[source] is { } moving && bag[target] is { } stack && StacksOnto(moving, stack, items))
                {
                    PlanBagMove(bag, source, target, items);
                }
            }
        }
    }

    private void OrderItems(BagItem?[] bag, IComparer<BagItem> comparer, ItemTable items)
    {
        var order = bag.OfType<BagItem>().OrderBy(item => item, comparer).ToList();

        for (var slot = 0; slot < order.Count; slot++)
        {
            var held = bag[slot];

            if (held != null && comparer.Compare(held, order[slot]) == 0)
            {
                continue;
            }

            var sources = Enumerable.Range(slot + 1, bag.Length - slot - 1)
                .Where(source => bag[source] is { } candidate && comparer.Compare(candidate, order[slot]) == 0)
                .ToList();
            var swapSource = sources.FirstOrDefault(
                source => held != null && source < order.Count && comparer.Compare(held, order[source]) == 0,
                sources[0]);

            PlanBagMove(bag, swapSource, slot, items);
        }
    }

    private void PlanBagMove(BagItem?[] bag, int source, int destination, ItemTable items)
    {
        var moving = bag[source]!;
        var move = new MoveBagItemCommand(_playerId, source, moving.Count, destination);

        if (bag[destination] is { } stack && StacksOnto(moving, stack, items)
            && items.TryGetById(stack.ItemId, out var item))
        {
            var moved = Math.Min(moving.Count, item.StackLimit - stack.Count);

            bag[destination] = stack with { Count = stack.Count + moved };
            bag[source] = moving.Count > moved ? moving with { Count = moving.Count - moved } : null;
        }
        else
        {
            bag[source] = bag[destination];
            bag[destination] = moving;
        }

        _bagSortSteps.Enqueue(new BagSortStep(move, bag.ToArray()));
    }

    private bool HoldsBag(IReadOnlyList<BagItem?> bag)
    {
        return Enumerable.Range(0, bag.Count)
            .All(slot => (bag[slot]?.ItemId, bag[slot]?.Count) == (_bag[slot]?.ItemId, _bag[slot]?.Count));
    }

    private static IComparer<BagItem> CreateBagSortComparer(BagSortKey key, ItemTable items)
    {
        ItemEntry? EntryOf(BagItem item) => items.TryGetById(item.ItemId, out var entry) ? entry : null;

        int RarityOf(BagItem item) => EntryOf(item) is { } entry ? ItemDescriber.RarityOf(entry, item) ?? -1 : -1;

        int ByCategory(BagItem first, BagItem second) =>
            SortCategoryOf(EntryOf(first)).CompareTo(SortCategoryOf(EntryOf(second)));

        int ByItemType(BagItem first, BagItem second) =>
            (EntryOf(first)?.Type ?? 0).CompareTo(EntryOf(second)?.Type ?? 0);

        int ByRarity(BagItem first, BagItem second) => RarityOf(second).CompareTo(RarityOf(first));

        int ByName(BagItem first, BagItem second) =>
            string.Compare(EntryOf(first)?.Name, EntryOf(second)?.Name, StringComparison.OrdinalIgnoreCase);

        int ByItemId(BagItem first, BagItem second) => first.ItemId.CompareTo(second.ItemId);

        int ByCountDescending(BagItem first, BagItem second) => second.Count.CompareTo(first.Count);

        Comparison<BagItem>[] criteria = key switch
        {
            BagSortKey.Type => [ByCategory, ByItemType, ByRarity, ByName, ByItemId, ByCountDescending],
            BagSortKey.Rarity => [ByRarity, ByCategory, ByItemType, ByName, ByItemId, ByCountDescending],
            _ => [ByName, ByItemId, ByRarity, ByCountDescending],
        };

        return Comparer<BagItem>.Create((first, second) =>
            criteria.Select(criterion => criterion(first, second)).FirstOrDefault(result => result != 0));
    }

    private static SortCategory SortCategoryOf(ItemEntry? item)
    {
        if (item == null)
        {
            return SortCategory.Material;
        }

        if (item.Type == QuestItemType)
        {
            return SortCategory.Quest;
        }

        var slots = FittingSlotsOf(item);

        if (slots.Count > 0)
        {
            return slots.All(slot => slot >= EquipmentSlot.HeadApparel)
                ? SortCategory.Apparel
                : SortCategory.Equipment;
        }

        return item.ScriptEffect != null ? SortCategory.Usable : SortCategory.Material;
    }

    private ItemEntry? FindEquipmentItem(int source, ItemTable items)
    {
        return FindBagItem(source, items, item => FittingSlotsOf(item).Count > 0);
    }

    private ItemEntry? FindUsableItem(int source, ItemTable items)
    {
        return FindBagItem(source, items, IsUsable);
    }

    private ItemEntry? FindBagItem(int source, ItemTable items, Func<ItemEntry, bool> isWanted)
    {
        return CanMoveBagItemFrom(source) && items.TryGetById(_bag[source]!.ItemId, out var item) && isWanted(item)
            ? item
            : null;
    }

    private EquipmentSlot DefaultSlotOf(ItemEntry item)
    {
        var slots = FittingSlotsOf(item);

        return slots.FirstOrDefault(slot => _equipment[(int)slot] == null, slots[0]);
    }

    private static EquipmentSlot? SecondSlotOf(ItemEntry item)
    {
        EquipmentSlot? second = item.Type switch
        {
            SwordItemType => EquipmentSlot.LeftHand,
            RingItemType => EquipmentSlot.Ring2,
            _ => null,
        };

        return second is { } slot && FittingSlotsOf(item).Contains(slot) ? slot : null;
    }

    private void ApplyEquipment(IReadOnlyList<BagItem?> equipment)
    {
        if (equipment.Count != EquipmentSlotCount)
        {
            Log.Warning($"the equipment has {EquipmentSlotCount} slots and the server sent {equipment.Count}");
        }

        for (var slot = 0; slot < EquipmentSlotCount; slot++)
        {
            _equipment[slot] = slot < equipment.Count ? equipment[slot] : null;
        }
    }

    private void ApplyBag(BagSync sync)
    {
        BagCapacity = sync.Capacity ?? BagCapacity;

        ApplySlots(_bag, sync.Slots, "bag");
    }

    private static void ApplySlots(BagItem?[] items, IReadOnlyList<BagSlot> slots, string bagName)
    {
        foreach (var slot in slots)
        {
            if (slot.Index < 0 || slot.Index >= items.Length)
            {
                Log.Warning($"the {bagName} has {items.Length} slots and the server filled slot {slot.Index}");

                continue;
            }

            items[slot.Index] = slot.Item;
        }
    }

    private record BagSortStep(MoveBagItemCommand Move, IReadOnlyList<BagItem?> Bag);

    private record ItemCooldown(DateTime StartedAt, TimeSpan Length);

    private enum SortCategory
    {
        Equipment,
        Apparel,
        Usable,
        Material,
        Quest,
    }
}
