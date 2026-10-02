using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Tools.Debug.MapOverlays;

public abstract class CellColorMeshBuilder(MapGrid grid, float lift) : ChunkMeshBuilder(grid)
{
    private readonly List<Vector3> _vertices = [];

    private readonly List<Color> _colors = [];

    private readonly List<int> _indices = [];

    public override Mesh? Build(Vector2I chunk)
    {
        var cells = FindCells(chunk);

        _vertices.Clear();
        _colors.Clear();
        _indices.Clear();

        for (var cellY = cells.Position.Y; cellY < cells.End.Y; cellY++)
        {
            for (var cellX = cells.Position.X; cellX < cells.End.X; cellX++)
            {
                if (TryPickColor(cellX, cellY, out var color))
                {
                    AddCell(chunk, cellX, cellY, color);
                }
            }
        }

        return _vertices.Count == 0
            ? null
            : CreateMesh(Mesh.PrimitiveType.Triangles, _vertices, _colors, _indices);
    }

    protected abstract bool TryPickColor(int cellX, int cellY, out Color color);

    private void AddCell(Vector2I chunk, int cellX, int cellY, Color color)
    {
        var corner00 = _vertices.Count;

        for (var cornerY = 0; cornerY <= 1; cornerY++)
        {
            for (var cornerX = 0; cornerX <= 1; cornerX++)
            {
                _vertices.Add(MeasureCornerPoint(chunk, cellX + cornerX, cellY + cornerY, lift));
                _colors.Add(color);
            }
        }

        var corner10 = corner00 + 1;
        var corner01 = corner00 + 2;
        var corner11 = corner00 + 3;

        _indices.AddRange([corner00, corner10, corner01, corner10, corner11, corner01]);
    }
}
