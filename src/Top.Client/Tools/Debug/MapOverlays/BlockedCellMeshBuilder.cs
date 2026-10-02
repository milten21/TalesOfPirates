using Godot;
using Top.Client.Features.Characters.Movement;
using Top.Client.Features.Maps;

namespace Top.Client.Tools.Debug.MapOverlays;

public class BlockedCellMeshBuilder(MapGrid grid, Walkability walkability) : CellColorMeshBuilder(grid, Lift)
{
    private const float Lift = 0.03f;

    private static readonly Color BlockedColor = new Color(1f, 0.1f, 0.1f, 0.45f);

    protected override bool TryPickColor(int cellX, int cellY, out Color color)
    {
        color = BlockedColor;

        return !walkability.IsWalkable(cellX, cellY);
    }
}
