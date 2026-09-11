using System;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Game.World.SceneObjects;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.World
{
    public class MapFactory : IMapFactory
    {
        private readonly MapTable _mapTable;
        private readonly IMapReader _mapReader;
        private readonly ISceneObjectFactory _sceneObjectFactory;
        private readonly Material _terrainMaterial;
        private readonly Material _waterMaterial;
        private readonly float _streamingRadius;

        public MapFactory(MapTable mapTable, IMapReader mapReader, ISceneObjectFactory sceneObjectFactory,
            Material terrainMaterial, Material waterMaterial, float streamingRadius)
        {
            _mapTable = mapTable ?? throw new ArgumentNullException(nameof(mapTable));
            _mapReader = mapReader ?? throw new ArgumentNullException(nameof(mapReader));
            _sceneObjectFactory = sceneObjectFactory ?? throw new ArgumentNullException(nameof(sceneObjectFactory));
            _terrainMaterial = terrainMaterial;
            _waterMaterial = waterMaterial;
            _streamingRadius = streamingRadius;
        }

        public async Task<MapInstance> Instantiate(int mapId, Transform parent,
            CancellationToken cancellationToken = default)
        {
            if (!_mapTable.TryGetById(mapId, out var mapEntry))
            {
                throw new ArgumentOutOfRangeException(nameof(mapId), mapId, "the map table names no such map");
            }

            var mapData = await _mapReader.Read(mapEntry.MapPath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return new MapInstance(mapEntry, mapData, parent, _terrainMaterial, _waterMaterial, _sceneObjectFactory,
                _streamingRadius);
        }
    }
}
