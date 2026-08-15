using System;
using System.Collections.Generic;
using System.Threading;
using Top.Client.Assets.Maps;
using Top.Client.Assets.Models;
using Top.Contracts.Assets.Maps;
using Top.Logging;
using UnityEngine;

namespace Top.Client.Presentation
{
    /// <summary>
    /// Handles the spawning and management of scenery objects within a map's chunk-based system.
    /// </summary>
    public class ScenerySpawner : IDisposable
    {
        private class ChunkScenery
        {
            public readonly CancellationTokenSource Cancel = new CancellationTokenSource();
            public readonly List<ModelInstance> Instances = new List<ModelInstance>();
        }

        private readonly MapData _map;
        private readonly ChunkWindow _chunks;
        private readonly Transform _parent;
        private readonly ISceneObjectCatalog _sceneObjects;
        private readonly Dictionary<Vector2Int, ChunkScenery> _spawned = new Dictionary<Vector2Int, ChunkScenery>();

        public ScenerySpawner(MapData map, ChunkWindow chunks, Transform parent,
            ISceneObjectCatalog sceneObjects)
        {
            _map = map;
            _chunks = chunks;
            _parent = parent;
            _sceneObjects = sceneObjects;

            _chunks.Added += Build;
            _chunks.Removed += Release;

            foreach (var chunk in _chunks.Chunks)
            {
                Build(chunk);
            }
        }

        public void Dispose()
        {
            _chunks.Added -= Build;
            _chunks.Removed -= Release;

            foreach (var scenery in _spawned.Values)
            {
                Release(scenery);
            }

            _spawned.Clear();
        }

        private void Build(Vector2Int chunk)
        {
            var scenery = new ChunkScenery();

            _spawned[chunk] = scenery;

            foreach (var placement in _map.PlacementsAt(chunk.x, chunk.y))
            {
                if (placement.Kind == PlacementKind.Model)
                {
                    Spawn(scenery, placement);
                }
            }
        }

        private async void Spawn(ChunkScenery scenery, MapPlacement placement)
        {
            try
            {
                var instance = await _sceneObjects.Spawn(placement.Id, _parent, scenery.Cancel.Token);

                if (instance == null)
                {
                    return;
                }

                if (scenery.Cancel.IsCancellationRequested)
                {
                    instance.Dispose();

                    return;
                }

                Seat(instance.Root, placement);
                scenery.Instances.Add(instance);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Log.Error($"scene object {placement.Id} broke while spawning", exception);
            }
        }

        public static float SeatHeight(MapData map, MapPlacement placement)
        {
            return Mathf.Max(map.HeightAt(placement.X, placement.Y), 0f) + placement.HeightOffset;
        }

        private void Seat(Transform root, MapPlacement placement)
        {
            root.localPosition = MapSpace.ToWorld(placement.X, placement.Y, SeatHeight(_map, placement));
            root.localRotation = Quaternion.Euler(0f, placement.Yaw - 180f, 0f);
        }

        private void Release(Vector2Int chunk)
        {
            if (_spawned.Remove(chunk, out var scenery))
            {
                Release(scenery);
            }
        }

        private static void Release(ChunkScenery scenery)
        {
            scenery.Cancel.Cancel();

            foreach (var instance in scenery.Instances)
            {
                instance.Dispose();
            }

            scenery.Instances.Clear();
        }
    }
}
