using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Tools.Debug.MapOverlays;

public partial class MapOverlay : Node3D, IChunkLoader
{
    private const int AboveWaterPriority = 1;

    private readonly Dictionary<Vector2I, MeshInstance3D> _loadedChunks = [];

    private MapGrid _grid = null!;

    private ChunkMeshBuilder _meshBuilder = null!;

    private Material _material = null!;

    public static MapOverlay Create(string name, MapGrid grid, ChunkMeshBuilder meshBuilder)
    {
        return new MapOverlay
        {
            Name = name,
            _grid = grid,
            _meshBuilder = meshBuilder,
            _material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                VertexColorUseAsAlbedo = true,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                RenderPriority = AboveWaterPriority
            }
        };
    }

    public void Load(Vector2I chunk)
    {
        if (_meshBuilder.Build(chunk) is not { } mesh)
        {
            return;
        }

        var chunkSize = _grid.ChunkSize;
        var instance = new MeshInstance3D
        {
            Name = $"chunk_{chunk.X}_{chunk.Y}",
            Position = MapSpace.ToWorld(chunk.X * chunkSize, chunk.Y * chunkSize, 0f),
            Mesh = mesh,
            MaterialOverride = _material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
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
}
