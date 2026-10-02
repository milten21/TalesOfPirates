using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Tools.Debug.MapOverlays;

public class CellGridMeshBuilder(MapGrid grid) : ChunkMeshBuilder(grid)
{
    private const float Lift = 0.04f;

    private static readonly Color TileEdgeColor = new Color(0f, 0f, 0f, 0.8f);

    private static readonly Color CellEdgeColor = new Color(0f, 0f, 0f, 0.35f);

    private readonly List<Vector3> _vertices = [];

    private readonly List<Color> _colors = [];

    public override Mesh Build(Vector2I chunk)
    {
        var cells = FindCells(chunk);

        _vertices.Clear();
        _colors.Clear();

        for (var cornerY = cells.Position.Y; cornerY < cells.End.Y; cornerY++)
        {
            var color = PickLineColor(cornerY);

            for (var cellX = cells.Position.X; cellX < cells.End.X; cellX++)
            {
                AddEdge(chunk, new Vector2I(cellX, cornerY), new Vector2I(cellX + 1, cornerY), color);
            }
        }

        for (var cornerX = cells.Position.X; cornerX < cells.End.X; cornerX++)
        {
            var color = PickLineColor(cornerX);

            for (var cellY = cells.Position.Y; cellY < cells.End.Y; cellY++)
            {
                AddEdge(chunk, new Vector2I(cornerX, cellY), new Vector2I(cornerX, cellY + 1), color);
            }
        }

        return CreateMesh(Mesh.PrimitiveType.Lines, _vertices, _colors);
    }

    private static Color PickLineColor(int lineCorner)
    {
        return lineCorner % MapGrid.CellsPerTile == 0 ? TileEdgeColor : CellEdgeColor;
    }

    private void AddEdge(Vector2I chunk, Vector2I from, Vector2I to, Color color)
    {
        _vertices.Add(MeasureCornerPoint(chunk, from.X, from.Y, Lift));
        _vertices.Add(MeasureCornerPoint(chunk, to.X, to.Y, Lift));
        _colors.Add(color);
        _colors.Add(color);
    }
}
