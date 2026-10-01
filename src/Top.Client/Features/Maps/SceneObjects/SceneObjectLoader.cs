using System;
using System.Collections.Generic;
using Godot;
using Top.Contracts.Assets.Maps;
using Top.Logging;

namespace Top.Client.Features.Maps.SceneObjects;

public partial class SceneObjectLoader : Node3D, IChunkLoader
{
    private const ulong BudgetMicroseconds = 4000;

    private class LoadingChunk
    {
        public Node3D Group = null!;
        public IReadOnlyList<MapPlacement> Placements = null!;
        public int Next;
    }

    private readonly Dictionary<Vector2I, LoadingChunk> _loadedChunks =
        new Dictionary<Vector2I, LoadingChunk>();

    private readonly List<Vector2I> _queue = [];

    private MapGrid _mapGrid = null!;
    private SceneObjectFactory _sceneObjectFactory = null!;

    public void Initialize(MapGrid mapGrid, SceneObjectFactory sceneObjectFactory)
    {
        _mapGrid = mapGrid;
        _sceneObjectFactory = sceneObjectFactory;
    }

    public void Load(Vector2I chunk)
    {
        var placements = _mapGrid.PlacementsAt(chunk.X, chunk.Y);

        if (placements.Count == 0)
        {
            return;
        }

        var group = new Node3D { Name = $"chunk_{chunk.X}_{chunk.Y}" };

        AddChild(group);

        _loadedChunks[chunk] = new LoadingChunk { Group = group, Placements = placements };
        _queue.Add(chunk);
    }

    public void Unload(Vector2I chunk)
    {
        if (_loadedChunks.Remove(chunk, out var loading))
        {
            _queue.Remove(chunk);
            loading.Group.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        var deadline = Time.GetTicksUsec() + BudgetMicroseconds;

        while (_queue.Count > 0 && Time.GetTicksUsec() < deadline)
        {
            if (!AddNext(_loadedChunks[_queue[0]]))
            {
                _queue.RemoveAt(0);
            }
        }
    }

    private bool AddNext(LoadingChunk loading)
    {
        while (loading.Next < loading.Placements.Count)
        {
            var placement = loading.Placements[loading.Next++];

            if (placement.Kind != PlacementKind.Model)
            {
                continue;
            }

            AddPlacement(loading.Group, placement);

            return true;
        }

        return false;
    }

    private void AddPlacement(Node3D group, MapPlacement placement)
    {
        Node3D? instance;

        try
        {
            instance = _sceneObjectFactory.Instantiate(placement.Id);
        }
        catch (Exception exception)
        {
            Log.Error($"scene object {placement.Id} broke while loading", exception);

            return;
        }

        if (instance == null)
        {
            return;
        }

        instance.Position = MapSpace
            .ToWorld(placement.X, placement.Y, _mapGrid.MeasurePlacementHeight(placement));
        instance.Rotation = new Vector3(0f, MapSpace.ToWorldYaw(placement.Yaw), 0f);

        group.AddChild(instance);
    }
}
