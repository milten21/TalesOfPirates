using System.Collections.Generic;
using Godot;
using Top.Contracts.Assets.Maps;
using Array = Godot.Collections.Array;

namespace Top.Client.Features.Maps.Terrain;

public partial class TerrainLoader : Node3D, IChunkLoader
{
    private const int TileLayers = 4;

    private static readonly Mesh.ArrayFormat CustomFormats =
        (Mesh.ArrayFormat)(((long)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift)
                           | ((long)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom1Shift));

    private readonly Dictionary<Vector2I, Node3D> _loadedChunks = new Dictionary<Vector2I, Node3D>();

    private readonly List<Vector3> _vertices = [];
    private readonly List<Vector3> _normals = [];
    private readonly List<Color> _colors = [];
    private readonly List<Vector2> _baseUv = [];
    private readonly List<Vector2> _mask1Uv = [];
    private readonly List<float> _mask23Uv = [];
    private readonly List<float> _slices = [];
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
        var chunkSize = _mapGrid.ChunkSize;
        var instance = new MeshInstance3D
        {
            Name = $"chunk_{chunk.X}_{chunk.Y}",
            Position = MapSpace.ToWorld(chunk.X * chunkSize, chunk.Y * chunkSize, 0f),
            Mesh = BuildMesh(chunk),
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

    private Mesh BuildMesh(Vector2I chunk)
    {
        var chunkSize = _mapGrid.ChunkSize;
        var layers = new MapTileLayer[TileLayers];

        Clear();

        if (_mapGrid.HasChunk(chunk.X, chunk.Y))
        {
            for (var localY = 0; localY < chunkSize; localY++)
            {
                for (var localX = 0; localX < chunkSize; localX++)
                {
                    var tileX = (chunk.X * chunkSize) + localX;
                    var tileY = (chunk.Y * chunkSize) + localY;

                    if (TryReadLayers(tileX, tileY, layers))
                    {
                        AddTile(localX, localY, tileX, tileY, layers);
                    }
                }
            }
        }
        else
        {
            AddOpenWater(chunkSize);
        }

        var arrays = new Array();

        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = _vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = _normals.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = _colors.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = _baseUv.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV2] = _mask1Uv.ToArray();
        arrays[(int)Mesh.ArrayType.Custom0] = _mask23Uv.ToArray();
        arrays[(int)Mesh.ArrayType.Custom1] = _slices.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = _indices.ToArray();

        var mesh = new ArrayMesh();

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, flags: CustomFormats);

        Clear();

        return mesh;
    }

    private bool TryReadLayers(int tileX, int tileY, MapTileLayer[] layers)
    {
        var ended = false;

        for (var i = 0; i < TileLayers; i++)
        {
            var layer = _mapGrid.LayerAt(tileX, tileY, i);
            var painted = !ended && layer.TerrainId != 0 && layer.MaskIndex != 0;

            ended |= layer.TerrainId == 0;
            layers[i] = painted ? layer : default;
        }

        return layers[0].TerrainId != 0;
    }

    private void AddTile(int localX, int localY, int tileX, int tileY, MapTileLayer[] layers)
    {
        var firstVertex = _vertices.Count;

        for (var cornerY = 0; cornerY <= 1; cornerY++)
        {
            for (var cornerX = 0; cornerX <= 1; cornerX++)
            {
                var height = _mapGrid.HeightAt(tileX + cornerX, tileY + cornerY);
                var mask2 = TerrainUv.MaskUvAt(layers[2].MaskIndex, cornerX, cornerY);
                var mask3 = TerrainUv.MaskUvAt(layers[3].MaskIndex, cornerX, cornerY);

                _vertices.Add(MapSpace.ToWorld(localX + cornerX, localY + cornerY, height));
                _normals.Add(MeasureNormal(tileX + cornerX, tileY + cornerY));
                _colors.Add(_mapGrid.ColorAt(tileX + cornerX, tileY + cornerY));
                _baseUv.Add(TerrainUv.BaseUvAt(tileX, tileY, cornerX, cornerY));
                _mask1Uv.Add(TerrainUv.MaskUvAt(layers[1].MaskIndex, cornerX, cornerY));

                _mask23Uv.Add(mask2.X);
                _mask23Uv.Add(mask2.Y);
                _mask23Uv.Add(mask3.X);
                _mask23Uv.Add(mask3.Y);

                _slices.Add(layers[0].TerrainId);
                _slices.Add(layers[1].TerrainId);
                _slices.Add(layers[2].TerrainId);
                _slices.Add(layers[3].TerrainId);
            }
        }

        AddQuad(firstVertex, firstVertex + 1, firstVertex + 2, firstVertex + 3);
    }

    private void AddOpenWater(int chunkSize)
    {
        var ground = MapTile.Underwater;
        var color = Color.Color8(ground.ColorR, ground.ColorG, ground.ColorB);

        for (var cornerY = 0; cornerY <= 1; cornerY++)
        {
            for (var cornerX = 0; cornerX <= 1; cornerX++)
            {
                _vertices.Add(MapSpace.ToWorld(cornerX * chunkSize, cornerY * chunkSize, ground.Height));
                _normals.Add(Vector3.Up);
                _colors.Add(color);
                _baseUv.Add(TerrainUv.BaseUvAtSpanCorner(chunkSize, cornerX, cornerY));
                _mask1Uv.Add(Vector2.Zero);

                _mask23Uv.AddRange([0f, 0f, 0f, 0f]);
                _slices.AddRange([(float)ground.Layer0.TerrainId, 0f, 0f, 0f]);
            }
        }

        AddQuad(0, 1, 2, 3);
    }

    private Vector3 MeasureNormal(int x, int y)
    {
        var slopeX = _mapGrid.HeightAt(x - 1, y) - _mapGrid.HeightAt(x + 1, y);
        var slopeY = _mapGrid.HeightAt(x, y - 1) - _mapGrid.HeightAt(x, y + 1);

        return MapSpace.ToWorld(slopeX, slopeY, 2f).Normalized();
    }

    private void AddQuad(int corner00, int corner10, int corner01, int corner11)
    {
        _indices.Add(corner00);
        _indices.Add(corner10);
        _indices.Add(corner01);
        _indices.Add(corner10);
        _indices.Add(corner11);
        _indices.Add(corner01);
    }

    private void Clear()
    {
        _vertices.Clear();
        _normals.Clear();
        _colors.Clear();
        _baseUv.Clear();
        _mask1Uv.Clear();
        _mask23Uv.Clear();
        _slices.Clear();
        _indices.Clear();
    }
}
