using System;
using System.Collections.Generic;
using UnityEngine;

namespace Top.Client.Assets.Maps
{
    /// <summary>
    /// The chunks within a radius of a consumer-set center point, updated as the
    /// center moves.
    /// </summary>
    public class ChunkWindow
    {
        private readonly MapData _map;
        private readonly float _radius;
        private readonly bool _populatedOnly;
        private readonly HashSet<Vector2Int> _chunks = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> _removed = new List<Vector2Int>();

        public ChunkWindow(MapData map, float radius, bool populatedOnly = true)
        {
            _map = map;
            _radius = radius;
            _populatedOnly = populatedOnly;
        }

        public event Action<Vector2Int> Added;
        public event Action<Vector2Int> Removed;

        public IReadOnlyCollection<Vector2Int> Chunks => _chunks;

        public void SetCenter(Vector2 center)
        {
            var inRange = ChunksInRange(center);

            _removed.Clear();

            foreach (var chunk in _chunks)
            {
                if (!inRange.Contains(chunk))
                {
                    _removed.Add(chunk);
                }
            }

            foreach (var chunk in _removed)
            {
                _chunks.Remove(chunk);
                Removed?.Invoke(chunk);
            }

            foreach (var chunk in inRange)
            {
                if (_chunks.Add(chunk))
                {
                    Added?.Invoke(chunk);
                }
            }
        }

        private HashSet<Vector2Int> ChunksInRange(Vector2 center)
        {
            var size = _map.ChunkSize;
            var minX = Mathf.Max(0, Mathf.FloorToInt((center.x - _radius) / size));
            var maxX = Mathf.Min(_map.ChunkCountX - 1, Mathf.FloorToInt((center.x + _radius) / size));
            var minY = Mathf.Max(0, Mathf.FloorToInt((center.y - _radius) / size));
            var maxY = Mathf.Min(_map.ChunkCountY - 1, Mathf.FloorToInt((center.y + _radius) / size));

            var inRange = new HashSet<Vector2Int>();

            for (var chunkY = minY; chunkY <= maxY; chunkY++)
            {
                for (var chunkX = minX; chunkX <= maxX; chunkX++)
                {
                    if ((!_populatedOnly || _map.HasChunk(chunkX, chunkY)) && IsInRange(center, chunkX, chunkY))
                    {
                        inRange.Add(new Vector2Int(chunkX, chunkY));
                    }
                }
            }

            return inRange;
        }

        private bool IsInRange(Vector2 center, int chunkX, int chunkY)
        {
            var size = _map.ChunkSize;
            var nearest = new Vector2(
                Mathf.Clamp(center.x, chunkX * size, (chunkX + 1) * size),
                Mathf.Clamp(center.y, chunkY * size, (chunkY + 1) * size));

            return (nearest - center).sqrMagnitude <= _radius * _radius;
        }
    }
}
