using System;
using System.IO;
using System.Threading;
using Top.Client.Game;
using Top.Client.Game.Tables;
using Top.Client.Game.World;
using Top.Client.Game.World.SceneObjects;
using Top.Client.Game.World.Terrain;
using Top.Client.Game.World.Water;
using Top.Client.Models;
using Top.Content;
using Top.Logging;
using UnityEngine;

namespace Top.Client.App
{
    public class MapPreview : MonoBehaviour
    {
        [SerializeField] private int _mapId = 1;
        [SerializeField] private Transform _focus;
        [SerializeField] private bool _followSceneView;
        [SerializeField] private Light _sun;
        [SerializeField] private float _mapStreamingRadius = 192f;
        [SerializeField] private ShaderSettings _shaders;

        private TerrainMaterial _terrainMaterial;
        private WaterMaterial _waterMaterial;
        private GameWorld _world;
        private CancellationTokenSource _cancellationTokenSource;

        private void OnEnable()
        {
            Load();
        }

        private void OnDisable()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _world?.Dispose();
            _world = null;

            _waterMaterial?.Dispose();
            _waterMaterial = null;

            _terrainMaterial?.Dispose();
            _terrainMaterial = null;
        }

        private void Update()
        {
            if (TryFocus(out var position))
            {
                _world?.SetCenter(position);
            }
        }

        private async void Load()
        {
            if (_shaders == null)
            {
                Log.Error("no shader settings assigned");

                return;
            }

            // TODO: Temp
            var contentRoot = Path.Combine(Application.dataPath, "..", "..", "..", "artifacts", "content");
            var cancellationTokenSource = new CancellationTokenSource();

            _cancellationTokenSource = cancellationTokenSource;

            try
            {
                var cancellationToken = cancellationTokenSource.Token;
                var contentSource = new FolderContentSource(contentRoot);
                var tableReader = new TableReader(contentSource);
                var tableSet = await tableReader.Read(cancellationTokenSource.Token);

                cancellationToken.ThrowIfCancellationRequested();

                var terrainMaterial = await TerrainMaterial.Load(contentSource, tableSet.TerrainTable, _shaders.Terrain,
                    cancellationToken);
                var waterMaterial = await WaterMaterial.Load(contentSource, _shaders.Water, cancellationToken);
                var mapReader = new MapReader(contentSource);
                var modelStore = new ModelStore(contentSource, _shaders.Model);
                var sceneObjectFactory = new SceneObjectFactory(tableSet.SceneObjectTable, modelStore);
                var mapFactory = new MapFactory(tableSet.MapTable, mapReader, sceneObjectFactory, terrainMaterial.Material,
                    waterMaterial.Material, _mapStreamingRadius);
                var world = new GameWorld(mapFactory, transform, _sun);

                await world.SetMap(_mapId, cancellationTokenSource.Token);

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    world.Dispose();
                    waterMaterial.Dispose();
                    terrainMaterial.Dispose();

                    return;
                }

                _terrainMaterial = terrainMaterial;
                _waterMaterial = waterMaterial;
                _world = world;

                Log.Info($"Streaming {world.Map.DisplayName} ({world.Map.MapPath}) from {contentRoot}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Log.Error($"map {_mapId} broke while loading", exception);
            }
        }

        private bool TryFocus(out Vector3 position)
        {
#if UNITY_EDITOR
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;

            if (_followSceneView && sceneView != null)
            {
                position = sceneView.camera.transform.position;

                return true;
            }
#endif
            position = _focus != null ? _focus.position : default;

            return _focus != null;
        }
    }
}
