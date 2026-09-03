using System;
using System.IO;
using System.Threading;
using Top.Client.Core;
using Top.Client.Game.Tables;
using Top.Client.Game.World;
using Top.Client.Game.World.SceneObjects;
using Top.Client.Game.World.Terrain;
using Top.Client.Game.World.Water;
using Top.Client.Models;
using Top.Content;
using Top.Contracts.Tables.World;
using Top.Logging;
using UnityEngine;

namespace Top.Client.App
{
    public class MapPreview : MonoBehaviour
    {
        [SerializeField] private int _mapId = 1;
        [SerializeField] private Transform _center;
        [SerializeField] private bool _followSceneView;
        [SerializeField] private Light _sun;
        [SerializeField] private float _radius = 192f;
        [SerializeField] private ShaderSettings _shaders;

        private ChunkWindow _allChunks;
        private ChunkWindow _populatedChunks;
        private MapStore _mapStore;
        private MapTerrain _terrain;
        private MapWater _water;
        private SceneObjectSpawner _sceneObjects;
        private Transform _terrainGroup;
        private Transform _waterGroup;
        private Transform _sceneObjectGroup;
        private CancellationTokenSource _cancel;

        private void OnEnable()
        {
            Show();
        }

        private void OnDisable()
        {
            _cancel?.Cancel();
            _cancel?.Dispose();
            _cancel = null;
            _sceneObjects?.Dispose();
            _sceneObjects = null;
            _terrain?.Dispose();
            _terrain = null;
            _water?.Dispose();
            _water = null;
            _mapStore?.Dispose();
            _mapStore = null;
            _allChunks = null;
            _populatedChunks = null;

            DestroyGroup(ref _terrainGroup);
            DestroyGroup(ref _waterGroup);
            DestroyGroup(ref _sceneObjectGroup);
        }

        private Transform Group(string groupName)
        {
            var group = new GameObject(groupName);

            group.transform.SetParent(transform, worldPositionStays: false);

            return group.transform;
        }

        private static void DestroyGroup(ref Transform group)
        {
            if (group != null)
            {
                UnityObjects.Destroy(group.gameObject);
            }

            group = null;
        }

        private async void Show()
        {
            if (_shaders == null)
            {
                Log.Error("no shader settings assigned");

                return;
            }

            // TODO: Temp
            var root = Path.Combine(Application.dataPath, "..", "..", "..", "artifacts", "content");
            var cancel = new CancellationTokenSource();

            _cancel = cancel;

            try
            {
                var content = new FolderContentSource(root);
                var tables = await new TableStore(content).Load(cancel.Token);

                if (!tables.Maps.TryGetById(_mapId, out var entry))
                {
                    Log.Error($"no map {_mapId} in the table");

                    return;
                }

                var mapStore = new MapStore(content, tables.Terrains.TexturePaths(), _shaders.Terrain, _shaders.Water);
                var map = await mapStore.Load(entry.MapPath, cancel.Token);

                if (!ReferenceEquals(_cancel, cancel))
                {
                    mapStore.Dispose();

                    return;
                }

                var terrain = await mapStore.GetTerrainMaterial(cancel.Token);
                var water = await mapStore.GetWaterMaterial(cancel.Token);

                if (!ReferenceEquals(_cancel, cancel))
                {
                    mapStore.Dispose();

                    return;
                }

                _mapStore = mapStore;

                ConfigureLighting(entry);

                _terrainGroup = Group("Terrain");
                _waterGroup = Group("Water");
                _sceneObjectGroup = Group("SceneObjects");

                _allChunks = new ChunkWindow(map, _radius, populatedOnly: false);
                _populatedChunks = new ChunkWindow(map, _radius);
                _terrain = new MapTerrain(map, _allChunks, _terrainGroup, terrain);
                _water = new MapWater(map, _allChunks, _waterGroup, water);
                _sceneObjects = new SceneObjectSpawner(map, _populatedChunks, _sceneObjectGroup,
                    new SceneObjectFactory(tables.SceneObjects, new ModelStore(content, _shaders.Model)));

                Log.Info($"Streaming {entry.DisplayName} ({entry.MapPath}) from {root}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Log.Error($"map {_mapId} broke while loading", exception);
            }
        }

        private void ConfigureLighting(MapEntry entry)
        {
            if (_sun == null)
            {
                return;
            }

            var lightDirection = entry.LightDirection;
            var direction = MapSpace.ToWorld(lightDirection.X, lightDirection.Y, lightDirection.Z);

            if (direction != Vector3.zero)
            {
                _sun.transform.rotation = Quaternion.LookRotation(direction);
            }

            _sun.color = entry.LightColor.ToUnity();
        }

        private void Update()
        {
            if (!TryCenter(out var position))
            {
                return;
            }

            var center = MapSpace.ToMap(position);

            _allChunks?.SetCenter(center);
            _populatedChunks?.SetCenter(center);
        }

        private bool TryCenter(out Vector3 position)
        {
#if UNITY_EDITOR
            var view = UnityEditor.SceneView.lastActiveSceneView;

            if (_followSceneView && view != null)
            {
                position = view.camera.transform.position;

                return true;
            }
#endif
            position = _center != null ? _center.position : default;

            return _center != null;
        }
    }
}
