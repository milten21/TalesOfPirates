using System.Collections.Generic;
using Godot;

namespace Top.Client.Features.Maps;

public class ChunkStreamer(MapGrid mapGrid, float streamingRadius, params IChunkLoader[] chunkLoaders)
{
    private readonly HashSet<Vector2I> _loadedChunks = [];
    private readonly HashSet<Vector2I> _chunksInRange = [];
    private readonly List<Vector2I> _chunksToUnload = [];
    private readonly List<IChunkLoader> _chunkLoaders = [.. chunkLoaders];

    private Vector2 _center;
    private bool _centered;

    public IReadOnlyCollection<Vector2I> Chunks => _loadedChunks;

    public void SetCenter(Vector2 mapCenter)
    {
        if (_centered && mapCenter == _center)
        {
            return;
        }

        _center = mapCenter;
        _centered = true;

        UpdateChunksInRange(mapCenter);

        _chunksToUnload.Clear();

        foreach (var chunk in _loadedChunks)
        {
            if (!_chunksInRange.Contains(chunk))
            {
                _chunksToUnload.Add(chunk);
            }
        }

        foreach (var chunk in _chunksToUnload)
        {
            _loadedChunks.Remove(chunk);
            UnloadChunk(chunk);
        }

        foreach (var chunk in _chunksInRange)
        {
            if (_loadedChunks.Add(chunk))
            {
                LoadChunk(chunk);
            }
        }
    }

    public void AddLoader(IChunkLoader chunkLoader)
    {
        _chunkLoaders.Add(chunkLoader);

        foreach (var chunk in _loadedChunks)
        {
            chunkLoader.Load(chunk);
        }
    }

    public void RemoveLoader(IChunkLoader chunkLoader)
    {
        if (!_chunkLoaders.Remove(chunkLoader))
        {
            return;
        }

        foreach (var chunk in _loadedChunks)
        {
            chunkLoader.Unload(chunk);
        }
    }

    public void UnloadAll()
    {
        foreach (var chunk in _loadedChunks)
        {
            UnloadChunk(chunk);
        }

        _loadedChunks.Clear();
        _centered = false;
    }

    private void LoadChunk(Vector2I chunk)
    {
        foreach (var loader in _chunkLoaders)
        {
            loader.Load(chunk);
        }
    }

    private void UnloadChunk(Vector2I chunk)
    {
        foreach (var loader in _chunkLoaders)
        {
            loader.Unload(chunk);
        }
    }

    private void UpdateChunksInRange(Vector2 center)
    {
        var chunkSize = mapGrid.ChunkSize;
        var minX = Mathf.Max(0, Mathf.FloorToInt((center.X - streamingRadius) / chunkSize));
        var maxX = Mathf.Min(mapGrid.ChunkCountX - 1, Mathf.FloorToInt((center.X + streamingRadius) / chunkSize));
        var minY = Mathf.Max(0, Mathf.FloorToInt((center.Y - streamingRadius) / chunkSize));
        var maxY = Mathf.Min(mapGrid.ChunkCountY - 1, Mathf.FloorToInt((center.Y + streamingRadius) / chunkSize));

        _chunksInRange.Clear();

        for (var chunkY = minY; chunkY <= maxY; chunkY++)
        {
            for (var chunkX = minX; chunkX <= maxX; chunkX++)
            {
                if (IsInRange(center, chunkX, chunkY))
                {
                    _chunksInRange.Add(new Vector2I(chunkX, chunkY));
                }
            }
        }
    }

    private bool IsInRange(Vector2 center, int chunkX, int chunkY)
    {
        var chunkSize = mapGrid.ChunkSize;
        var nearest = new Vector2(
            Mathf.Clamp(center.X, chunkX * chunkSize, (chunkX + 1) * chunkSize),
            Mathf.Clamp(center.Y, chunkY * chunkSize, (chunkY + 1) * chunkSize));

        return (nearest - center).LengthSquared() <= streamingRadius * streamingRadius;
    }
}
