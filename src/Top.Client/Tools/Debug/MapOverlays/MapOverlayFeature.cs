using System;
using Godot;
using Top.Client.Features.Characters.Movement;
using Top.Client.Features.Maps;
using Top.Client.Features.World;

namespace Top.Client.Tools.Debug.MapOverlays;

public class MapOverlayFeature(string displayName, Func<MapGrid, Walkability, ChunkMeshBuilder> createMeshBuilder)
    : DebugFeature
{
    private Map? _map;

    private MapOverlay? _overlay;

    public override string DisplayName => displayName;

    public override DebugGroup Group => DebugGroup.Map;

    public override bool IsAvailable(Node? scene)
    {
        return scene is WorldScene { Walkability: not null };
    }

    protected override void Enable(Node? scene)
    {
        if (scene is not WorldScene { Walkability: { } walkability } world)
        {
            return;
        }

        var grid = world.Map.Grid;

        _map = world.Map;
        _overlay = MapOverlay.Create(displayName, grid, createMeshBuilder(grid, walkability));

        _map.AddChild(_overlay);
        _map.AddChunkLoader(_overlay);
    }

    protected override void Disable()
    {
        if (GodotObject.IsInstanceValid(_map) && _overlay != null)
        {
            _map.RemoveChunkLoader(_overlay);
        }

        if (GodotObject.IsInstanceValid(_overlay))
        {
            _overlay.QueueFree();
        }

        _map = null;
        _overlay = null;
    }
}
