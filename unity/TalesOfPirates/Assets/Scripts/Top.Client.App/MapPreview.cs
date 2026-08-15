using System;
using System.IO;
using System.Threading;
using Top.Client.Assets.Maps;
using Top.Client.Assets.Models;
using Top.Client.Core;
using Top.Client.Definitions;
using Top.Client.Presentation;
using Top.Content.Packs;
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

        private ChunkWindow _allChunks;
        private ChunkWindow _populatedChunks;
        private MapStore _maps;
        private MapTerrain _terrain;
        private MapWater _water;
        private ScenerySpawner _scenery;
        private Transform _terrainGroup;
        private Transform _waterGroup;
        private Transform _sceneryGroup;
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
            _scenery?.Dispose();
            _scenery = null;
            _terrain?.Dispose();
            _terrain = null;
            _water?.Dispose();
            _water = null;
            _maps?.Dispose();
            _maps = null;
            _allChunks = null;
            _populatedChunks = null;

            DestroyGroup(ref _terrainGroup);
            DestroyGroup(ref _waterGroup);
            DestroyGroup(ref _sceneryGroup);
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
            // TODO: Temp
            var root = Path.Combine(Application.dataPath, "..", "..", "..", "artifacts", "content");
            var cancel = new CancellationTokenSource();

            _cancel = cancel;

            try
            {
                var content = new FolderContent(root);
                var definitions = await new DefinitionsStore(content).Load(cancel.Token);

                if (!definitions.Maps.TryGetById(_mapId, out var entry))
                {
                    Log.Error($"no map {_mapId} in the table");

                    return;
                }

                var maps = new MapStore(content, definitions.Terrains.TexturePaths());
                var map = await maps.Load(entry.MapPath, cancel.Token);

                if (!ReferenceEquals(_cancel, cancel))
                {
                    maps.Dispose();

                    return;
                }

                var terrain = await maps.GetTerrainMaterial(cancel.Token);
                var water = await maps.GetWaterMaterial(cancel.Token);

                if (!ReferenceEquals(_cancel, cancel))
                {
                    maps.Dispose();

                    return;
                }

                _maps = maps;

                ConfigureLighting(entry);

                _terrainGroup = Group("Terrain");
                _waterGroup = Group("Water");
                _sceneryGroup = Group("Scenery");

                _allChunks = new ChunkWindow(map, _radius, populatedOnly: false);
                _populatedChunks = new ChunkWindow(map, _radius);
                _terrain = new MapTerrain(map, _allChunks, _terrainGroup, terrain);
                _water = new MapWater(map, _allChunks, _waterGroup, water);
                _scenery = new ScenerySpawner(map, _populatedChunks, _sceneryGroup,
                    new SceneObjectCatalog(definitions.SceneObjects, new ModelStore(content)));

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

            if (entry.LightDirection != null && entry.LightDirection.Length == 3)
            {
                var direction = MapSpace.ToWorld(entry.LightDirection[0], entry.LightDirection[1],
                    entry.LightDirection[2]);

                if (direction != Vector3.zero)
                {
                    _sun.transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            if (entry.LightColor != null && entry.LightColor.Length == 3)
            {
                _sun.color = new Color(entry.LightColor[0], entry.LightColor[1], entry.LightColor[2]);
            }
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
