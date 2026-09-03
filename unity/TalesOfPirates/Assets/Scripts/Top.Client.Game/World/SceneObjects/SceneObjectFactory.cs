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
        private readonly SceneObjectTable _sceneObjects;
        private readonly IModelStore _models;

        public SceneObjectFactory(SceneObjectTable sceneObjects, IModelStore models)
        {
            _sceneObjects = sceneObjects ?? throw new ArgumentNullException(nameof(sceneObjects));
            _models = models ?? throw new ArgumentNullException(nameof(models));
        }

        public Task<ModelInstance> Build(int id, Transform parent, CancellationToken cancel = default)
        {
            if (!_sceneObjects.TryGetById(id, out var entry)
                || entry.Kind != SceneObjectKind.Model
                || string.IsNullOrEmpty(entry.ModelPath))
            {
                return Task.FromResult<ModelInstance>(null);
            }

            return _models.Spawn(entry.ModelPath, parent, cancel);
        }
    }
}
