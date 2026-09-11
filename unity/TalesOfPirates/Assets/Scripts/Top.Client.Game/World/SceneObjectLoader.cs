using System;
using System.Collections.Generic;
using System.Threading;
using Top.Client.Game.World.SceneObjects;
using Top.Client.Models;
using Top.Contracts.Assets.Maps;
using Top.Logging;
using UnityEngine;

namespace Top.Client.Game.World
{
    public class SceneObjectLoader : IChunkLoader
    {
        private class LoadedChunk
        {
            public readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
            public readonly List<ModelInstance> ModelInstances = new List<ModelInstance>();
        }

        private readonly MapData _mapData;
        private readonly Transform _parent;
        private readonly ISceneObjectFactory _sceneObjectFactory;
        private readonly Dictionary<Vector2Int, LoadedChunk> _loadedChunks = new Dictionary<Vector2Int, LoadedChunk>();

        public SceneObjectLoader(MapData mapData, Transform parent, ISceneObjectFactory sceneObjectFactory)
        {
            _mapData = mapData;
            _parent = parent;
            _sceneObjectFactory = sceneObjectFactory;
        }

        public void Load(Vector2Int chunk)
        {
            LoadedChunk loadedChunk = null;

            foreach (var mapPlacement in _mapData.PlacementsAt(chunk.x, chunk.y))
            {
                if (mapPlacement.Kind != PlacementKind.Model)
                {
                    continue;
                }

                loadedChunk ??= _loadedChunks[chunk] = new LoadedChunk();

                Add(loadedChunk, mapPlacement);
            }
        }

        public void Unload(Vector2Int chunk)
        {
            if (_loadedChunks.Remove(chunk, out var loaded))
            {
                Discard(loaded);
            }
        }

        private async void Add(LoadedChunk loadedChunk, MapPlacement mapPlacement)
        {
            try
            {
                var instance = await _sceneObjectFactory
                    .Instantiate(mapPlacement.Id, _parent, loadedChunk.CancellationTokenSource.Token);

                if (instance == null)
                {
                    return;
                }

                if (loadedChunk.CancellationTokenSource.IsCancellationRequested)
                {
                    instance.Dispose();

                    return;
                }

                Place(instance.Root, mapPlacement);

                loadedChunk.ModelInstances.Add(instance);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Log.Error($"scene object {mapPlacement.Id} broke while loading", exception);
            }
        }

        private void Place(Transform root, MapPlacement mapPlacement)
        {
            root.localPosition = MapSpace
                .ToWorld(mapPlacement.X, mapPlacement.Y, _mapData.GetPlacementHeight(mapPlacement));

            root.localRotation = Quaternion.Euler(0f, mapPlacement.Yaw - 180f, 0f);
        }

        private static void Discard(LoadedChunk loadedChunk)
        {
            loadedChunk.CancellationTokenSource.Cancel();

            foreach (var instance in loadedChunk.ModelInstances)
            {
                instance.Dispose();
            }

            loadedChunk.ModelInstances.Clear();
        }
    }
}
