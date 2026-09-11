using System;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Models;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.World.SceneObjects
{
    public class SceneObjectFactory : ISceneObjectFactory
    {
        private readonly SceneObjectTable _sceneObjectTable;
        private readonly IModelStore _modelStore;

        public SceneObjectFactory(SceneObjectTable sceneObjectTable, IModelStore modelStore)
        {
            _sceneObjectTable = sceneObjectTable ?? throw new ArgumentNullException(nameof(sceneObjectTable));
            _modelStore = modelStore ?? throw new ArgumentNullException(nameof(modelStore));
        }

        public Task<ModelInstance> Instantiate(int id, Transform parent, CancellationToken cancellationToken = default)
        {
            if (!_sceneObjectTable.TryGetById(id, out var entry)
                || entry.Kind != SceneObjectKind.Model
                || string.IsNullOrEmpty(entry.ModelPath))
            {
                return Task.FromResult<ModelInstance>(null);
            }

            return _modelStore.Instantiate(entry.ModelPath, parent, cancellationToken);
        }
    }
}
