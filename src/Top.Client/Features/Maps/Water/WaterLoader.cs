using System.Collections.Generic;
using Godot;
using Array = Godot.Collections.Array;

namespace Top.Client.Features.Maps.Water;

public partial class WaterLoader : Node3D, IChunkLoader
{
    private const int QuadTiles = 4;
    private const float FadeDepth = -0.5f;

    private static readonly Color WaterColor = Color.Color8(140, 140, 220, 207);
    private static readonly Color FadedColor = Color.Color8(255, 255, 255, 0);

    private readonly Dictionary<Vector2I, Node3D> _loadedChunks = new Dictionary<Vector2I, Node3D>();

    private readonly List<Vector3> _vertices = [];
    private readonly List<Color> _colors = [];
    private readonly List<Vector2> _uv = [];
    private readonly List<int> _indices = [];

    private MapGrid _mapGrid = null!;
    private Material _material = null!;

    public void Initialize(MapGrid mapGrid, Material material)
    {
        _mapGrid = mapGrid;
        _material = material;
    }

    public void Load(Vector2I chunk)
    {
        var mesh = BuildMesh(chunk);

        if (mesh == null)
        {
            return;
        }

        var chunkSize = _mapGrid.ChunkSize;
        var instance = new MeshInstance3D
        {
            Name = $"water_{chunk.X}_{chunk.Y}",
            Position = MapSpace.ToWorld(chunk.X * chunkSize, chunk.Y * chunkSize, 0f),
            Mesh = mesh,
            MaterialOverride = _material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };

        AddChild(instance);

        _loadedChunks[chunk] = instance;
    }

    public void Unload(Vector2I chunk)
    {
        if (_loadedChunks.Remove(chunk, out var instance))
        {
            instance.QueueFree();
        }
    }

    private Mesh? BuildMesh(Vector2I chunk)
    {
        var quadsPerEdge = _mapGrid.ChunkSize / QuadTiles;
        var verticesPerRow = quadsPerEdge + 1;
        var visible = false;

        Clear();

        for (var y = 0; y <= quadsPerEdge; y++)
        {
            for (var x = 0; x <= quadsPerEdge; x++)
            {
                var mapX = ((chunk.X * quadsPerEdge) + x) * QuadTiles;
                var mapY = ((chunk.Y * quadsPerEdge) + y) * QuadTiles;
                var color = ReadVertexColor(mapX, mapY);

                visible |= color.A != 0f;

                _vertices.Add(MapSpace.ToWorld(x * QuadTiles, y * QuadTiles, 0f));
                _colors.Add(color);
                _uv.Add(new Vector2(mapX / (float)QuadTiles, mapY / (float)QuadTiles));
            }
        }

        if (!visible)
        {
            Clear();

            return null;
        }

        for (var y = 0; y < quadsPerEdge; y++)
        {
            for (var x = 0; x < quadsPerEdge; x++)
            {
                var firstVertex = (y * verticesPerRow) + x;

                _indices.Add(firstVertex);
                _indices.Add(firstVertex + 1);
                _indices.Add(firstVertex + verticesPerRow);
                _indices.Add(firstVertex + 1);
                _indices.Add(firstVertex + verticesPerRow + 1);
                _indices.Add(firstVertex + verticesPerRow);
            }
        }

        var arrays = new Array();

        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = _colors.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = _uv.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = _indices.ToArray();

        var mesh = new ArrayMesh();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        Clear();

        return mesh;
    }

    private Color ReadVertexColor(int mapX, int mapY)
    {
        return _mapGrid.HeightAt(mapX, mapY) > FadeDepth ? FadedColor : WaterColor;
    }

    private void Clear()
    {
        _vertices.Clear();
        _colors.Clear();
        _uv.Clear();
        _indices.Clear();
    }
}
