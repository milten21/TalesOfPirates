using System;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Core;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.World
{
    public class GameWorld : IDisposable
    {
        private readonly IMapFactory _mapFactory;
        private readonly Transform _root;
        private readonly Light _sun;

        private MapInstance _mapInstance;

        public GameWorld(IMapFactory mapFactory, Transform root, Light sun)
        {
            _mapFactory = mapFactory ?? throw new ArgumentNullException(nameof(mapFactory));
            _root = root;
            _sun = sun;
        }

        public MapEntry Map => _mapInstance?.MapEntry;

        public async Task SetMap(int mapId, CancellationToken cancellationToken = default)
        {
            var mapInstance = await _mapFactory.Instantiate(mapId, _root, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                mapInstance.Dispose();

                return;
            }

            _mapInstance?.Dispose();
            _mapInstance = mapInstance;

            ApplySunlight(mapInstance.MapEntry);
        }

        public void SetCenter(Vector3 worldPosition)
        {
            _mapInstance?.SetCenter(worldPosition);
        }

        public void Dispose()
        {
            _mapInstance?.Dispose();
            _mapInstance = null;
        }

        private void ApplySunlight(MapEntry entry)
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
    }
}
