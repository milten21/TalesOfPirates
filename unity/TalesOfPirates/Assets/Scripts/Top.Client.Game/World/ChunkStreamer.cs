using System;
using System.Collections.Generic;
using UnityEngine;

namespace Top.Client.Game.World
{
    public class ChunkStreamer : IDisposable
    {
        private readonly MapData _mapData;
        private readonly float _streamingRadius;
        private readonly IChunkLoader[] _chunkLoaders;
        private readonly HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> _chunksInRange = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _chunksToUnload = new List<Vector2Int>();

        private Vector2 _center;
        private bool _centered;

        public ChunkStreamer(MapData mapData, float streamingRadius, params IChunkLoader[] chunkLoaders)
        {
            _mapData = mapData;
            _streamingRadius = streamingRadius;
            _chunkLoaders = chunkLoaders ?? Array.Empty<IChunkLoader>();
        }

        public IReadOnlyCollection<Vector2Int> Chunks => _loadedChunks;

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

        public void Dispose()
        {
            foreach (var chunk in _loadedChunks)
            {
                UnloadChunk(chunk);
            }

            _loadedChunks.Clear();
            _centered = false;
        }

        private void LoadChunk(Vector2Int chunk)
        {
            foreach (var loader in _chunkLoaders)
            {
                loader.Load(chunk);
            }
        }

        private void UnloadChunk(Vector2Int chunk)
        {
            foreach (var loader in _chunkLoaders)
            {
                loader.Unload(chunk);
            }
        }

        private void UpdateChunksInRange(Vector2 center)
        {
            var chunkSize = _mapData.ChunkSize;
            var minX = Mathf.Max(0, Mathf.FloorToInt((center.x - _streamingRadius) / chunkSize));
            var maxX = Mathf.Min(_mapData.ChunkCountX - 1, Mathf.FloorToInt((center.x + _streamingRadius) / chunkSize));
            var minY = Mathf.Max(0, Mathf.FloorToInt((center.y - _streamingRadius) / chunkSize));
            var maxY = Mathf.Min(_mapData.ChunkCountY - 1, Mathf.FloorToInt((center.y + _streamingRadius) / chunkSize));

            _chunksInRange.Clear();

            for (var chunkY = minY; chunkY <= maxY; chunkY++)
            {
                for (var chunkX = minX; chunkX <= maxX; chunkX++)
                {
                    if (IsInRange(center, chunkX, chunkY))
                    {
                        _chunksInRange.Add(new Vector2Int(chunkX, chunkY));
                    }
                }
            }
        }

        private bool IsInRange(Vector2 center, int chunkX, int chunkY)
        {
            var chunkSize = _mapData.ChunkSize;
            var nearest = new Vector2(
                Mathf.Clamp(center.x, chunkX * chunkSize, (chunkX + 1) * chunkSize),
                Mathf.Clamp(center.y, chunkY * chunkSize, (chunkY + 1) * chunkSize));

            return (nearest - center).sqrMagnitude <= _streamingRadius * _streamingRadius;
        }
    }
}
