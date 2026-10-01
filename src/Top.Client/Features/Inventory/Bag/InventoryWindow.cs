using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Top.Client.Features.Inventory.Tooltip;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Textures;
using Top.Client.UI;
using Top.Contracts.Tables.Items;

namespace Top.Client.Features.Inventory.Bag;

public partial class InventoryWindow : PanelContainer
{
    private const string SlotGlyphFolder = "res://Features/Inventory/Assets/SlotGlyphs";

    private const int EquipmentColumnSlotCount = 7;

    private const int CompanionSlotCount = 4;

    private const double TimedTooltipRefreshSeconds = 1.0;

    private const string SplitTitle = "Split stack";

    private const string SplitConfirmText = "Split";

    private const int SortMenuGap = 6;

    private const string DeleteTitle = "Delete item";

    private const string DeleteConfirmText = "Delete";

    private const string ThrowTitle = "Throw item";

    private const string ThrowConfirmText = "Throw";

    private const string CancelText = "Cancel";

    private static readonly StringName UnlockedActionVariation = "WindowAction";

    private static readonly StringName LockedActionVariation = "WindowActionLocked";

    private static readonly EquipmentLayout GearLayout = new(
        [
            EquipmentSlot.Head, EquipmentSlot.Necklace, EquipmentSlot.Body, EquipmentSlot.Gloves, EquipmentSlot.Belt,
            EquipmentSlot.Shoes, EquipmentSlot.Cloak,
        ],
        [
            EquipmentSlot.RightHand, EquipmentSlot.LeftHand, EquipmentSlot.Handguard, EquipmentSlot.Ring1,
            EquipmentSlot.Ring2, EquipmentSlot.Bracelet1, EquipmentSlot.Bracelet2,
        ],
        [EquipmentSlot.Wings, EquipmentSlot.Fairy, EquipmentSlot.RearFairy, EquipmentSlot.Mount]);

    private static readonly EquipmentLayout ApparelLayout = new(
        [
            EquipmentSlot.HeadApparel, EquipmentSlot.FaceApparel, EquipmentSlot.BodyApparel,
            EquipmentSlot.GlovesApparel, EquipmentSlot.ShoesApparel, EquipmentSlot.FairyApparel,
            EquipmentSlot.GlowApparel,
        ],
        [
            EquipmentSlot.DaggerApparel, EquipmentSlot.SwordApparel, EquipmentSlot.GreatswordApparel,
            EquipmentSlot.OffHandSwordApparel, EquipmentSlot.ShieldApparel, EquipmentSlot.GunApparel,
            EquipmentSlot.BowApparel,
        ],
        [EquipmentSlot.StaffApparel]);

    private readonly List<ItemCell> _leftColumnCells = [];

    private readonly List<ItemCell> _rightColumnCells = [];

    private readonly List<ItemCell> _companionCells = [];

    private readonly List<ItemCell> _bagCells = [];

    private readonly List<ItemCell> _temporaryBagCells = [];

    private readonly Dictionary<ItemCell, Func<HoveredSlot>> _slotsByCell = [];

    private ItemTable _itemTable = null!;

    private ItemDescriber _itemDescriber = null!;

    private CharacterInventory? _inventory;

    private CharacterState? _player;

    private EquipmentLayout _equipmentLayout = GearLayout;

    private IReadOnlyList<BagItem?> _equipment = new BagItem?[CharacterInventory.EquipmentSlotCount];

    private IReadOnlyList<BagItem?> _bag = new BagItem?[CharacterInventory.BagSlotCount];

    private IReadOnlyList<BagItem?> _temporaryBag = new BagItem?[CharacterInventory.TemporaryBagSlotCount];

    private int _bagCapacity;

    private ItemCell? _hoveredCell;

    private bool _isHoveredItemTimed;

    private double _tooltipAge;

    private bool _isDragging;

    private bool _isBagLocked;

    private ItemCell? _draggedCell;

    private int? _draggedBagSlot;

    private EquipmentSlot? _draggedEquipmentSlot;

    private int? _draggedTemporaryBagSlot;

    private Action<int>? _confirmQuantity;

    private Action? _confirmItemAction;

    private int? _menuBagSlot;

    [Signal]
    public delegate void BagLockRequestedEventHandler();

    [Signal]
    public delegate void BagUnlockRequestedEventHandler(string code);

    [Signal]
    public delegate void UnlockCodeRejectedEventHandler(string error);

    [Signal]
    public delegate void BagItemMoveRequestedEventHandler(int source, int destination);

    [Signal]
    public delegate void BagItemSplitRequestedEventHandler(int source, int count, int destination);

    [Signal]
    public delegate void BagSortRequestedEventHandler(int key);

    [Signal]
    public delegate void BagItemUseRequestedEventHandler(int source);

    [Signal]
    public delegate void BagItemEquipRequestedEventHandler(int source);

    [Signal]
    public delegate void BagItemEquipToSlotRequestedEventHandler(int source, int slot);

    [Signal]
    public delegate void ItemUnequipRequestedEventHandler(int slot, int destination);

    [Signal]
    public delegate void TemporaryBagItemTakeRequestedEventHandler(int source, int destination);

    [Signal]
    public delegate void TemporaryBagItemSplitRequestedEventHandler(int source, int count, int destination);

    [Signal]
    public delegate void TemporaryBagTakeAllRequestedEventHandler();

    [Signal]
    public delegate void BagItemDeleteRequestedEventHandler(int source);

    [Signal]
    public delegate void BagItemThrowRequestedEventHandler(int source, int count);

    [Export] public Control TitleBar { get; set; } = null!;

    [Export] public Button CloseButton { get; set; } = null!;

    [Export] public Button GearTab { get; set; } = null!;

    [Export] public Button ApparelTab { get; set; } = null!;

    [Export] public Container LeftColumn { get; set; } = null!;

    [Export] public Container RightColumn { get; set; } = null!;

    [Export] public Container CompanionRow { get; set; } = null!;

    [Export] public GridContainer BagGrid { get; set; } = null!;

    [Export] public Label BagCount { get; set; } = null!;

    [Export] public GridContainer TemporaryBagGrid { get; set; } = null!;

    [Export] public Label TemporaryBagCount { get; set; } = null!;

    [Export] public Label GoldAmount { get; set; } = null!;

    [Export] public ItemTooltipGroup Tooltip { get; set; } = null!;

    [Export] public PackedScene ItemCellScene { get; set; } = null!;

    [Export] public Button LockButton { get; set; } = null!;

    [Export] public Label LockIcon { get; set; } = null!;

    [Export] public Label LockCaption { get; set; } = null!;

    [Export] public BagUnlockForm UnlockForm { get; set; } = null!;

    [Export] public QuantityPopup QuantityPopup { get; set; } = null!;

    [Export] public ConfirmPopup ConfirmPopup { get; set; } = null!;

    [Export] public Control DropRing { get; set; } = null!;

    [Export] public Button TakeAllButton { get; set; } = null!;

    [Export] public Button SortButton { get; set; } = null!;

    [Export] public Control SortMenu { get; set; } = null!;

    [Export] public Button SortByTypeButton { get; set; } = null!;

    [Export] public Button SortByRarityButton { get; set; } = null!;

    [Export] public Button SortByNameButton { get; set; } = null!;

    [Export] public Control ItemMenu { get; set; } = null!;

    [Export] public Button MenuUseButton { get; set; } = null!;

    [Export] public Button MenuEquipButton { get; set; } = null!;

    [Export] public Button MenuDeleteButton { get; set; } = null!;

    [Export] public Button MenuThrowButton { get; set; } = null!;

    [Export] public Button MenuLockButton { get; set; } = null!;

    [Export] public Button MenuUnlockButton { get; set; } = null!;

    public override void _Ready()
    {
        CloseButton.Pressed += Hide;
        TitleBar.GuiInput += DragByTitleBar;
        GearTab.Pressed += () => ShowEquipmentLayout(GearLayout);
        ApparelTab.Pressed += () => ShowEquipmentLayout(ApparelLayout);
        VisibilityChanged += ShowTooltip;
        VisibilityChanged += SortMenu.Hide;
        VisibilityChanged += CloseItemMenu;
        LockButton.Pressed += PressLock;
        UnlockForm.UnlockRequested += code => EmitSignal(SignalName.BagUnlockRequested, code);
        UnlockForm.CodeRejected += error => EmitSignal(SignalName.UnlockCodeRejected, error);
        QuantityPopup.Confirmed += count => _confirmQuantity?.Invoke(count);
        ConfirmPopup.Confirmed += () => _confirmItemAction?.Invoke();
        TakeAllButton.Pressed += () => EmitSignal(SignalName.TemporaryBagTakeAllRequested);
        SortButton.Pressed += ToggleSortMenu;
        SortByTypeButton.Pressed += () => SortBag(BagSortKey.Type);
        SortByRarityButton.Pressed += () => SortBag(BagSortKey.Rarity);
        SortByNameButton.Pressed += () => SortBag(BagSortKey.Name);
        MenuUseButton.Pressed += () => RunMenuAction(slot => EmitSignal(SignalName.BagItemUseRequested, slot));
        MenuEquipButton.Pressed += () => RunMenuAction(slot => EmitSignal(SignalName.BagItemEquipRequested, slot));
        MenuDeleteButton.Pressed += () => RunMenuAction(ConfirmDelete);
        MenuThrowButton.Pressed += () => RunMenuAction(ConfirmThrow);
        MenuLockButton.Pressed += () => RunMenuAction(_ => PressLock());
        MenuUnlockButton.Pressed += () => RunMenuAction(_ => PressLock());
    }

    public override void _Input(InputEvent received)
    {
        if (received is not InputEventMouseButton { Pressed: true } click)
        {
            return;
        }

        if (SortMenu.Visible && !SortMenu.GetGlobalRect().HasPoint(click.Position)
                             && !SortButton.GetGlobalRect().HasPoint(click.Position))
        {
            SortMenu.Hide();
        }

        if (ItemMenu.Visible && !ItemMenu.GetGlobalRect().HasPoint(click.Position))
        {
            CloseItemMenu();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationDragEnd)
        {
            ThrowItemDraggedOut();
            EndItemDrag();
        }

        if (what is (int)NotificationDragBegin or (int)NotificationDragEnd)
        {
            ShowTooltip();
            ShowDropRing();
        }
    }

    public override void _Process(double delta)
    {
        ShowCooldowns();

        if (ItemMenu.Visible)
        {
            ShowItemMenuEntries();
        }

        if (!_isHoveredItemTimed)
        {
            return;
        }

        _tooltipAge += delta;

        if (_tooltipAge >= TimedTooltipRefreshSeconds)
        {
            ShowTooltip();
        }
    }

    public void Setup(ItemTable itemTable, ItemDescriber itemDescriber, TextureCache textureCache)
    {
        _itemTable = itemTable;
        _itemDescriber = itemDescriber;

        AddCells(LeftColumn, _leftColumnCells, EquipmentColumnSlotCount, textureCache,
            index => EquippedAt(_equipmentLayout.LeftColumn, index));
        AddCells(RightColumn, _rightColumnCells, EquipmentColumnSlotCount, textureCache,
            index => EquippedAt(_equipmentLayout.RightColumn, index));
        AddCells(CompanionRow, _companionCells, CompanionSlotCount, textureCache,
            index => EquippedAt(_equipmentLayout.CompanionRow, index));
        AllowEquipmentDrags(_leftColumnCells, layout => layout.LeftColumn);
        AllowEquipmentDrags(_rightColumnCells, layout => layout.RightColumn);
        AllowEquipmentDrags(_companionCells, layout => layout.CompanionRow);
        AddCells(BagGrid, _bagCells, CharacterInventory.BagSlotCount, textureCache,
            index => new HoveredSlot(ItemPlace.Bag, _bag[index], index >= _bagCapacity));
        AllowBagDrags();
        AddCells(TemporaryBagGrid, _temporaryBagCells, CharacterInventory.TemporaryBagSlotCount, textureCache,
            index => new HoveredSlot(ItemPlace.TemporaryBag, _temporaryBag[index], false));
        AllowTemporaryBagDrags();

        ShowEquipmentLayout(_equipmentLayout);
    }

    public void ShowInventory(CharacterInventory inventory)
    {
        _inventory = inventory;
        _equipment = inventory.Equipment;
        _bag = inventory.Bag;
        _temporaryBag = inventory.TemporaryBag;
        _bagCapacity = inventory.BagCapacity;

        ShowEquipment();
        ShowItems(_bagCells, inventory.Bag, inventory.BagCapacity);
        ShowItems(_temporaryBagCells, inventory.TemporaryBag, CharacterInventory.TemporaryBagSlotCount);

        BagCount.Text = ToCountText(inventory.Bag, inventory.BagCapacity);
        TemporaryBagCount.Text = ToCountText(inventory.TemporaryBag, CharacterInventory.TemporaryBagSlotCount);
        TakeAllButton.Disabled = !inventory.CanTakeAllTemporaryBagItems;
        SortButton.Disabled = !inventory.CanSortBag;
        SortMenu.Visible &= inventory.CanSortBag;

        ShowBagLock(inventory.IsBagLocked, inventory.IsUnlockingBag);
        ShowItemMenuEntries();
        ShowTooltip();
        ShowDropRing();
        ShowEquipmentDropHints();
    }

    public void RejectUnlockCode()
    {
        UnlockForm.RejectCode();
    }

    public void ShowPlayer(CharacterState player)
    {
        _player = player;
        GoldAmount.Text = player.Gold.ToString("N0", CultureInfo.InvariantCulture);

        ShowTooltip();
    }

    private static string ToCountText(IReadOnlyList<BagItem?> items, int capacity)
    {
        var used = items.Take(capacity).Count(item => item != null);

        return string.Create(CultureInfo.InvariantCulture, $"{used} / {capacity}");
    }

    private static bool IsExpired(BagItem? item)
    {
        return item is { Expiration: not 0 } && item.Expiration <= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static Texture2D LoadSlotGlyph(EquipmentSlot slot)
    {
        return GD.Load<Texture2D>($"{SlotGlyphFolder}/{slot}.png");
    }

    private void ShowBagLock(bool isLocked, bool isUnlocking)
    {
        if (isLocked && !_isBagLocked)
        {
            UnlockForm.Clear();
        }

        _isBagLocked = isLocked;

        LockButton.ThemeTypeVariation = isLocked ? LockedActionVariation : UnlockedActionVariation;
        LockIcon.Text = isLocked ? "lock" : "lock_open";
        LockCaption.Text = isLocked ? "Locked" : "Lock";

        UnlockForm.Visible = isLocked;
        UnlockForm.ShowWaiting(isUnlocking);
    }

    private void ShowCooldowns()
    {
        if (_inventory == null || !IsVisibleInTree())
        {
            return;
        }

        var now = DateTime.UtcNow;

        for (var index = 0; index < _bagCells.Count; index++)
        {
            _bagCells[index].ShowCooldown(_bag[index] is { } item ? _inventory.CooldownLeftOf(item.ItemId, now) : 0f);
        }
    }

    private void ToggleSortMenu()
    {
        if (SortMenu.Visible)
        {
            SortMenu.Hide();

            return;
        }

        var buttonRect = SortButton.GetGlobalRect();

        SortMenu.ResetSize();
        SortMenu.GlobalPosition = new Vector2(buttonRect.End.X - SortMenu.Size.X, buttonRect.End.Y + SortMenuGap);
        SortMenu.Show();
    }

    private void SortBag(BagSortKey key)
    {
        SortMenu.Hide();
        EmitSignal(SignalName.BagSortRequested, (int)key);
    }

    private void PressLock()
    {
        if (_isBagLocked)
        {
            UnlockForm.FocusCode();
        }
        else
        {
            EmitSignal(SignalName.BagLockRequested);
        }
    }

    private void ShowEquipmentLayout(EquipmentLayout layout)
    {
        _equipmentLayout = layout;

        ShowSlotGlyphs(_leftColumnCells, layout.LeftColumn);
        ShowSlotGlyphs(_rightColumnCells, layout.RightColumn);
        ShowSlotGlyphs(_companionCells, layout.CompanionRow);
        ShowEquipment();
        ShowEquipmentDropHints();
        ShowTooltip();
    }

    private void ShowFittingTab(int bagSlot)
    {
        var slots = FittingSlotsAt(bagSlot);

        if (slots.Count == 0)
        {
            return;
        }

        if (ApparelLayout.Contains(slots[0]))
        {
            ApparelTab.ButtonPressed = true;
            ShowEquipmentLayout(ApparelLayout);
        }
        else if (GearLayout.Contains(slots[0]))
        {
            GearTab.ButtonPressed = true;
            ShowEquipmentLayout(GearLayout);
        }
    }

    private IReadOnlyList<EquipmentSlot> FittingSlotsAt(int bagSlot)
    {
        return _bag[bagSlot] is { } instance && FindItem(instance.ItemId) is { } item
            ? CharacterInventory.FittingSlotsOf(item)
            : [];
    }

    private void ShowEquipmentDropHints()
    {
        var isEquipmentDrag = _draggedBagSlot is { } source && FittingSlotsAt(source).Count > 0;

        ShowDropHints(_leftColumnCells, _equipmentLayout.LeftColumn, isEquipmentDrag);
        ShowDropHints(_rightColumnCells, _equipmentLayout.RightColumn, isEquipmentDrag);
        ShowDropHints(_companionCells, _equipmentLayout.CompanionRow, isEquipmentDrag);
    }

    private void ShowDropHints(List<ItemCell> cells, IReadOnlyList<EquipmentSlot> slots, bool isEquipmentDrag)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            var isFitting = isEquipmentDrag && index < slots.Count && CanEquipDraggedItem(slots[index]);

            cells[index].ShowDropRing(isFitting);
            cells[index].ShowDimmed(isEquipmentDrag && !isFitting);
        }
    }

    private bool CanEquipDraggedItem(EquipmentSlot? slot)
    {
        return slot is { } target && _draggedBagSlot is { } source && _inventory != null
               && _inventory.CanEquipBagItem(source, target, _itemTable);
    }

    private static void ShowSlotGlyphs(List<ItemCell> cells, IReadOnlyList<EquipmentSlot> slots)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            cells[index].Visible = index < slots.Count;
            cells[index].ShowGlyph(index < slots.Count ? LoadSlotGlyph(slots[index]) : null);
        }
    }

    private void ShowEquipment()
    {
        ShowEquippedItems(_leftColumnCells, _equipmentLayout.LeftColumn);
        ShowEquippedItems(_rightColumnCells, _equipmentLayout.RightColumn);
        ShowEquippedItems(_companionCells, _equipmentLayout.CompanionRow);
    }

    private void ShowEquippedItems(List<ItemCell> cells, IReadOnlyList<EquipmentSlot> slots)
    {
        for (var index = 0; index < slots.Count; index++)
        {
            ShowItemIn(cells[index], _equipment[(int)slots[index]]);
        }
    }

    private HoveredSlot EquippedAt(IReadOnlyList<EquipmentSlot> slots, int index)
    {
        return new HoveredSlot(ItemPlace.Equipment, index < slots.Count ? _equipment[(int)slots[index]] : null,
            false);
    }

    private void AddCells(Container container, List<ItemCell> cells, int count, TextureCache textureCache,
        Func<int, HoveredSlot> slotAt)
    {
        for (var index = 0; index < count; index++)
        {
            var cell = ItemCellScene.Instantiate<ItemCell>();
            var slotIndex = index;

            cell.Setup(textureCache);
            cell.MouseEntered += () => HoverCell(cell);
            cell.MouseExited += () => LeaveCell(cell);
            container.AddChild(cell);
            cells.Add(cell);
            _slotsByCell[cell] = () => slotAt(slotIndex);
        }
    }

    private void AllowBagDrags()
    {
        for (var index = 0; index < _bagCells.Count; index++)
        {
            var slot = index;

            _bagCells[index].SetDragForwarding(
                Callable.From<Vector2, Variant>(_ => DragBagItem(slot)),
                Callable.From<Vector2, Variant, bool>((_, _) => CanDropOnBagSlot(slot)),
                Callable.From<Vector2, Variant>((_, _) => DropOnBagSlot(slot)));
            _bagCells[index].GuiInput += received => ClickBagItem(slot, received);
        }
    }

    private void AllowTemporaryBagDrags()
    {
        for (var index = 0; index < _temporaryBagCells.Count; index++)
        {
            var slot = index;

            _temporaryBagCells[index].SetDragForwarding(
                Callable.From<Vector2, Variant>(_ => DragTemporaryBagItem(slot)),
                Callable.From<Vector2, Variant, bool>((_, _) => false),
                Callable.From<Vector2, Variant>((_, _) => { }));
        }
    }

    private void AllowEquipmentDrags(List<ItemCell> cells, Func<EquipmentLayout, IReadOnlyList<EquipmentSlot>> column)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            var cell = cells[index];
            var position = index;

            EquipmentSlot? SlotAt()
            {
                var slots = column(_equipmentLayout);

                return position < slots.Count ? slots[position] : null;
            }

            cell.SetDragForwarding(
                Callable.From<Vector2, Variant>(_ => DragEquippedItem(cell, SlotAt())),
                Callable.From<Vector2, Variant, bool>((_, _) => CanEquipDraggedItem(SlotAt())),
                Callable.From<Vector2, Variant>((_, _) => DropOnEquipmentSlot(SlotAt())));
        }
    }

    private Variant DragBagItem(int slot)
    {
        if (_inventory == null || !_inventory.CanMoveBagItemFrom(slot))
        {
            return default;
        }

        StartItemDrag(_bagCells[slot]);
        _draggedBagSlot = slot;
        ShowFittingTab(slot);
        ShowEquipmentDropHints();

        return slot;
    }

    private Variant DragTemporaryBagItem(int slot)
    {
        if (_inventory == null || !_inventory.CanTakeTemporaryBagItemFrom(slot))
        {
            return default;
        }

        StartItemDrag(_temporaryBagCells[slot]);
        _draggedTemporaryBagSlot = slot;

        return slot;
    }

    private Variant DragEquippedItem(ItemCell cell, EquipmentSlot? slot)
    {
        if (slot is not { } equipped || _inventory == null || !_inventory.CanUnequipItemFrom(equipped))
        {
            return default;
        }

        StartItemDrag(cell);
        _draggedEquipmentSlot = equipped;

        return (int)equipped;
    }

    private void StartItemDrag(ItemCell cell)
    {
        cell.SetDragPreview(cell.BuildDragPreview());
        cell.ShowDragged(true);
        _draggedCell = cell;
    }

    private void EndItemDrag()
    {
        _draggedCell?.ShowDragged(false);
        _draggedCell = null;
        _draggedBagSlot = null;
        _draggedEquipmentSlot = null;
        _draggedTemporaryBagSlot = null;

        ShowEquipmentDropHints();
    }

    private void ClickBagItem(int slot, InputEvent received)
    {
        if (received is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true })
        {
            var signal = FittingSlotsAt(slot).Count > 0
                ? SignalName.BagItemEquipRequested
                : SignalName.BagItemUseRequested;

            _bagCells[slot].AcceptEvent();
            EmitSignal(signal, slot);
        }
        else if (received is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } click
                 && slot < _bagCapacity && _bag[slot] != null)
        {
            _bagCells[slot].AcceptEvent();
            OpenItemMenu(slot, click.GlobalPosition);
        }
    }

    private void OpenItemMenu(int slot, Vector2 position)
    {
        _menuBagSlot = slot;

        ShowItemMenuEntries();
        ItemMenu.GlobalPosition = position.Clamp(Vector2.Zero, GetViewportRect().Size - ItemMenu.Size);
        ItemMenu.Show();
        ShowTooltip();
    }

    private void CloseItemMenu()
    {
        _menuBagSlot = null;

        ItemMenu.Hide();
        ShowTooltip();
    }

    private void ShowItemMenuEntries()
    {
        if (_menuBagSlot is not { } slot || _inventory == null)
        {
            return;
        }

        if (_bag[slot] is not { } instance)
        {
            CloseItemMenu();

            return;
        }

        MenuUseButton.Visible = FindItem(instance.ItemId) is { } item && CharacterInventory.IsUsable(item);
        MenuUseButton.Disabled = !_inventory.CanUseBagItem(slot, _itemTable, DateTime.UtcNow);
        MenuEquipButton.Visible = FittingSlotsAt(slot).Count > 0;
        MenuEquipButton.Disabled = !_inventory.CanEquipBagItem(slot, _itemTable);
        MenuDeleteButton.Disabled = !_inventory.CanDeleteBagItem(slot, _itemTable);
        MenuThrowButton.Disabled = !_inventory.CanThrowBagItem(slot, _itemTable);
        MenuLockButton.Disabled = _isBagLocked;
        MenuUnlockButton.Disabled = !_isBagLocked;
        ItemMenu.ResetSize();
    }

    private void RunMenuAction(Action<int> action)
    {
        if (_menuBagSlot is not { } slot)
        {
            return;
        }

        CloseItemMenu();
        action(slot);
    }

    private void ConfirmDelete(int slot)
    {
        if (_bag[slot] is not { } instance || FindItem(instance.ItemId) is not { } item)
        {
            return;
        }

        var name = instance.Count > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{instance.Count} × {item.Name}")
            : item.Name;

        OpenConfirmPopup(DeleteTitle, $"Delete {name}? It will be gone for good.", DeleteConfirmText,
            () => EmitSignal(SignalName.BagItemDeleteRequested, slot));
    }

    private void ThrowItemDraggedOut()
    {
        var viewport = GetViewport();

        if (_draggedBagSlot is { } slot && !viewport.GuiIsDragSuccessful() && viewport.GuiGetHoveredControl() == null)
        {
            ConfirmThrow(slot);
        }
    }

    private void ConfirmThrow(int slot)
    {
        if (_inventory == null || !_inventory.CanThrowBagItem(slot, _itemTable) || _bag[slot] is not { } instance
            || FindItem(instance.ItemId) is not { } item)
        {
            return;
        }

        if (instance.Count == 1)
        {
            OpenConfirmPopup(ThrowTitle, $"Throw {item.Name} away?", ThrowConfirmText,
                () => EmitSignal(SignalName.BagItemThrowRequested, slot, 1));

            return;
        }

        _confirmQuantity = count => EmitSignal(SignalName.BagItemThrowRequested, slot, count);

        QuantityPopup.Open(ThrowTitle,
            string.Create(CultureInfo.InvariantCulture, $"Throw how many {item.Name}? 1 to {instance.Count}."),
            ThrowConfirmText, instance.Count, instance.Count, true);
    }

    private void OpenConfirmPopup(string title, string question, string confirmText, Action confirm)
    {
        _confirmItemAction = confirm;

        ConfirmPopup.Open(title, question, confirmText, CancelText, true);
    }

    private void DropOnEquipmentSlot(EquipmentSlot? slot)
    {
        if (slot is { } target && _draggedBagSlot is { } source)
        {
            EmitSignal(SignalName.BagItemEquipToSlotRequested, source, (int)target);
        }
    }

    private void ShowDropRing()
    {
        var target = _hoveredCell != null ? _bagCells.IndexOf(_hoveredCell) : -1;

        DropRing.Visible = target >= 0 && CanDropOnBagSlot(target);

        if (DropRing.Visible)
        {
            DropRing.GlobalPosition = _bagCells[target].GlobalPosition;
            DropRing.Size = _bagCells[target].Size;
        }
    }

    private bool CanDropOnBagSlot(int slot)
    {
        if (_inventory == null)
        {
            return false;
        }

        if (_draggedEquipmentSlot is { } equipped)
        {
            return _inventory.CanUnequipItem(equipped, slot);
        }

        if (_draggedTemporaryBagSlot is { } temporarySource)
        {
            return _inventory.CanTakeTemporaryBagItem(temporarySource, slot, _itemTable);
        }

        return _draggedBagSlot is { } source && _inventory.CanMoveBagItem(source, slot);
    }

    private void DropOnBagSlot(int slot)
    {
        if (_draggedEquipmentSlot is { } equipped)
        {
            EmitSignal(SignalName.ItemUnequipRequested, (int)equipped, slot);

            return;
        }

        if (_draggedTemporaryBagSlot is { } temporarySource)
        {
            DropTemporaryBagItemOnBagSlot(temporarySource, slot);

            return;
        }

        if (_draggedBagSlot is not { } source)
        {
            return;
        }

        if (Input.IsKeyPressed(Key.Shift) && _inventory is { } inventory && inventory.CanSplitBagItem(source, slot)
            && inventory.Bag[source] is { } stack)
        {
            OpenSplitPopup(stack.Count,
                count => EmitSignal(SignalName.BagItemSplitRequested, source, count, slot));

            return;
        }

        EmitSignal(SignalName.BagItemMoveRequested, source, slot);
    }

    private void DropTemporaryBagItemOnBagSlot(int source, int slot)
    {
        if (Input.IsKeyPressed(Key.Shift) && _inventory is { } inventory
                                          && inventory.CanSplitTemporaryBagItem(source, slot, _itemTable)
                                          && inventory.TemporaryBag[source] is { } stack)
        {
            OpenSplitPopup(stack.Count,
                count => EmitSignal(SignalName.TemporaryBagItemSplitRequested, source, count, slot));

            return;
        }

        EmitSignal(SignalName.TemporaryBagItemTakeRequested, source, slot);
    }

    private void OpenSplitPopup(int stackCount, Action<int> confirmSplit)
    {
        _confirmQuantity = confirmSplit;

        QuantityPopup.Open(SplitTitle,
            string.Create(CultureInfo.InvariantCulture, $"Move how many? 1 to {stackCount - 1}."),
            SplitConfirmText, stackCount / 2, stackCount - 1, false);
    }

    private void ShowItems(List<ItemCell> cells, IReadOnlyList<BagItem?> items, int capacity)
    {
        for (var index = 0; index < cells.Count; index++)
        {
            cells[index].ShowLocked(index >= capacity);
            ShowItemIn(cells[index], items[index]);
        }
    }

    private void ShowItemIn(ItemCell cell, BagItem? instance)
    {
        var item = instance != null ? FindItem(instance.ItemId) : null;

        cell.ShowItem(item, instance?.Count ?? 0, IsExpired(instance));

        if (item != null && instance != null)
        {
            cell.ShowMark(_itemDescriber.ToneOf(item, instance), _itemDescriber.GemColorOf(item));
        }
        else
        {
            cell.ShowMark(ItemNameTone.White, 0);
        }
    }

    private void HoverCell(ItemCell cell)
    {
        _hoveredCell = cell;

        ShowTooltip();
        ShowDropRing();
    }

    private void LeaveCell(ItemCell cell)
    {
        if (_hoveredCell != cell)
        {
            return;
        }

        _hoveredCell = null;

        ShowTooltip();
        ShowDropRing();
    }

    private void ShowTooltip()
    {
        _tooltipAge = 0.0;
        _isHoveredItemTimed = false;

        if (_hoveredCell == null || !IsVisibleInTree() || _player == null || GetViewport().GuiIsDragging()
            || ItemMenu.Visible)
        {
            Tooltip.Hide();

            return;
        }

        var slot = _slotsByCell[_hoveredCell]();
        var cellRect = _hoveredCell.GetGlobalRect();

        if (slot.IsLocked)
        {
            Tooltip.ShowLockedSlot(cellRect);
        }
        else if (slot.Item != null && FindItem(slot.Item.ItemId) is { } item)
        {
            var now = DateTimeOffset.UtcNow;
            var description = _itemDescriber.Describe(item, slot.Item, slot.Place, _player, now);
            var equippedItems = slot.Place != ItemPlace.Equipment
                ? DescribeEquippedItems(item, _player, now)
                : [];

            _isHoveredItemTimed = description.Expiry != null
                                  || equippedItems.Any(equipped => equipped.Description.Expiry != null);
            Tooltip.ShowItem(description, equippedItems, cellRect);
        }
        else
        {
            Tooltip.Hide();
        }
    }

    private List<(string SlotName, ItemDescription Description)> DescribeEquippedItems(ItemEntry item,
        CharacterState player, DateTimeOffset now)
    {
        var equippedItems = new List<(string SlotName, ItemDescription Description)>();

        foreach (var slot in _inventory?.FilledFittingSlotsOf(item) ?? [])
        {
            if (_equipment[(int)slot] is { } instance && FindItem(instance.ItemId) is { } equipped)
            {
                equippedItems.Add((ItemDescriber.SlotNameOf(slot),
                    _itemDescriber.Describe(equipped, instance, ItemPlace.Equipment, player, now)));
            }
        }

        return equippedItems;
    }

    private ItemEntry? FindItem(int itemId)
    {
        return _itemTable.TryGetById(itemId, out var item) ? item : null;
    }

    private void DragByTitleBar(InputEvent received)
    {
        if (received is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            _isDragging = button.Pressed;
            TitleBar.AcceptEvent();
        }
        else if (received is InputEventMouseMotion motion && _isDragging)
        {
            Position = (Position + motion.Relative).Clamp(Vector2.Zero, GetParentAreaSize() - Size);
            TitleBar.AcceptEvent();
        }
    }

    private record HoveredSlot(ItemPlace Place, BagItem? Item, bool IsLocked);

    private record EquipmentLayout(
        IReadOnlyList<EquipmentSlot> LeftColumn,
        IReadOnlyList<EquipmentSlot> RightColumn,
        IReadOnlyList<EquipmentSlot> CompanionRow)
    {
        public bool Contains(EquipmentSlot slot)
        {
            return LeftColumn.Contains(slot) || RightColumn.Contains(slot) || CompanionRow.Contains(slot);
        }
    }
}
