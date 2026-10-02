using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Tools.Debug.MapOverlays;

public class CellHeightMeshBuilder(MapGrid grid) : CellColorMeshBuilder(grid, Lift)
{
    private const float Lift = 0.02f;

    private const float LowestHue = 2f / 3f;

    private const float Saturation = 0.9f;

    private const float Opacity = 0.45f;

    protected override bool TryPickColor(int cellX, int cellY, out Color color)
    {
        var height = Grid.CellHeightAt(cellX, cellY);
        var heightFraction = (height + MapGrid.MaximumCellHeight) / (2f * MapGrid.MaximumCellHeight);

        color = Color.FromHsv(LowestHue * (1f - heightFraction), Saturation, 1f, Opacity);

        return true;
    }
}
