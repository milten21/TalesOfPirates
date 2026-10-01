using System.Collections.Generic;
using Godot;

namespace Top.Client.Features.Inventory.Tooltip;

public partial class ItemTooltipGroup : HBoxContainer
{
    private const float CellGap = 8f;

    private const float LowerScreenShare = 0.55f;

    private readonly List<ItemTooltip> _equippedTooltips = [];

    private Rect2 _cell;

    [Export] public ItemTooltip HoveredTooltip { get; set; } = null!;

    [Export] public Container EquippedTooltips { get; set; } = null!;

    [Export] public PackedScene ItemTooltipScene { get; set; } = null!;

    public override void _Ready()
    {
        MinimumSizeChanged += PlaceBesideCell;
    }

    public void ShowItem(ItemDescription description,
        IReadOnlyList<(string SlotName, ItemDescription Description)> equippedItems, Rect2 cell)
    {
        HoveredTooltip.ShowItem(description);
        ShowEquippedItems(equippedItems);
        Place(cell);
    }

    public void ShowLockedSlot(Rect2 cell)
    {
        HoveredTooltip.ShowLockedSlot();
        ShowEquippedItems([]);
        Place(cell);
    }

    private void ShowEquippedItems(IReadOnlyList<(string SlotName, ItemDescription Description)> equippedItems)
    {
        while (_equippedTooltips.Count < equippedItems.Count)
        {
            var tooltip = ItemTooltipScene.Instantiate<ItemTooltip>();

            EquippedTooltips.AddChild(tooltip);
            _equippedTooltips.Add(tooltip);
        }

        for (var index = 0; index < _equippedTooltips.Count; index++)
        {
            if (index < equippedItems.Count)
            {
                _equippedTooltips[index].ShowEquippedItem(equippedItems[index].Description,
                    equippedItems[index].SlotName);
            }
            else
            {
                _equippedTooltips[index].Hide();
            }
        }

        EquippedTooltips.Visible = equippedItems.Count > 0;
    }

    private void Place(Rect2 cell)
    {
        _cell = cell;

        Show();
        PlaceBesideCell();
    }

    private void PlaceBesideCell()
    {
        ResetSize();

        var size = Size;
        var screen = GetViewportRect().Size;
        var isRightOfCell = _cell.End.X + CellGap + size.X <= screen.X;
        var isBottomAligned = _cell.Position.Y > screen.Y * LowerScreenShare;
        var alignment = isBottomAligned ? SizeFlags.ShrinkEnd : SizeFlags.ShrinkBegin;

        MoveChild(HoveredTooltip, isRightOfCell ? 0 : -1);
        HoveredTooltip.SizeFlagsVertical = alignment;
        EquippedTooltips.SizeFlagsVertical = alignment;

        var x = isRightOfCell ? _cell.End.X + CellGap : _cell.Position.X - CellGap - size.X;
        var y = isBottomAligned ? _cell.End.Y - size.Y : _cell.Position.Y;

        GlobalPosition = new Vector2(Mathf.Max(x, 0f), Mathf.Clamp(y, 0f, Mathf.Max(screen.Y - size.Y, 0f)));
    }
}
