using System;
using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;
using Array = Godot.Collections.Array;

namespace Top.Client.Tools.Debug.MapOverlays;

public abstract class ChunkMeshBuilder(MapGrid grid)
{
    private const float CellSize = 1f / MapGrid.CellsPerTile;

    protected MapGrid Grid => grid;

    public abstract Mesh? Build(Vector2I chunk);

    protected static ArrayMesh CreateMesh(Mesh.PrimitiveType primitive, List<Vector3> vertices, List<Color> colors,
        List<int>? indices = null)
    {
        var arrays = new Array();

        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();

        if (indices != null)
        {
            arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
        }

        var mesh = new ArrayMesh();

        mesh.AddSurfaceFromArrays(primitive, arrays);

        return mesh;
    }

    protected Rect2I FindCells(Vector2I chunk)
    {
        var firstTile = chunk * grid.ChunkSize;
        var tileEnd = new Vector2I(
            Math.Min(firstTile.X + grid.ChunkSize, grid.TileCountX),
            Math.Min(firstTile.Y + grid.ChunkSize, grid.TileCountY));

        return new Rect2I(firstTile * MapGrid.CellsPerTile, (tileEnd - firstTile) * MapGrid.CellsPerTile);
    }

    protected Vector3 MeasureCornerPoint(Vector2I chunk, int cornerX, int cornerY, float lift)
    {
        var mapX = cornerX * CellSize;
        var mapY = cornerY * CellSize;
        var chunkOrigin = chunk * grid.ChunkSize;

        return MapSpace.ToWorld(mapX - chunkOrigin.X, mapY - chunkOrigin.Y, grid.HeightAt(mapX, mapY) + lift);
    }
}
