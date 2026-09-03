using System;
using System.Collections.Generic;
using System.Threading;
using Top.Client.Models;
using Top.Contracts.Assets.Maps;
using Top.Logging;
using UnityEngine;

namespace Top.Client.Game.World.SceneObjects
{
    public class SceneObjectSpawner : IDisposable
    {
        private class ChunkSceneObjects
        {
            public readonly CancellationTokenSource Cancel = new CancellationTokenSource();
            public readonly List<ModelInstance> Instances = new List<ModelInstance>();
        }

        private readonly MapData _map;
        private readonly ChunkWindow _chunks;
        private readonly Transform _parent;
        private readonly ISceneObjectFactory _factory;

        private readonly Dictionary<Vector2Int, ChunkSceneObjects> _spawned =
            new Dictionary<Vector2Int, ChunkSceneObjects>();

        public SceneObjectSpawner(MapData map, ChunkWindow chunks, Transform parent, ISceneObjectFactory factory)
        {
            _map = map;
            _chunks = chunks;
            _parent = parent;
            _factory = factory;

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

            foreach (var sceneObjects in _spawned.Values)
            {
                Release(sceneObjects);
            }

            _spawned.Clear();
        }

        private void Build(Vector2Int chunk)
        {
            var sceneObjects = new ChunkSceneObjects();

            _spawned[chunk] = sceneObjects;

            foreach (var placement in _map.PlacementsAt(chunk.x, chunk.y))
            {
                if (placement.Kind == PlacementKind.Model)
                {
                    Spawn(sceneObjects, placement);
                }
            }
        }

        private async void Spawn(ChunkSceneObjects sceneObjects, MapPlacement placement)
        {
            try
            {
                var instance = await _factory.Build(placement.Id, _parent, sceneObjects.Cancel.Token);

                if (instance == null)
                {
                    return;
                }

                if (sceneObjects.Cancel.IsCancellationRequested)
                {
                    instance.Dispose();

                    return;
                }

                Seat(instance.Root, placement);
                sceneObjects.Instances.Add(instance);
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
            if (_spawned.Remove(chunk, out var sceneObjects))
            {
                Release(sceneObjects);
            }
        }

        private static void Release(ChunkSceneObjects sceneObjects)
        {
            sceneObjects.Cancel.Cancel();

            foreach (var instance in sceneObjects.Instances)
            {
                instance.Dispose();
            }

            sceneObjects.Instances.Clear();
        }
    }
}
