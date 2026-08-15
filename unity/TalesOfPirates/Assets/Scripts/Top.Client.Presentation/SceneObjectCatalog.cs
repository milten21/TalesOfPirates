using System;
using System.Threading;
using System.Threading.Tasks;
using Top.Client.Assets.Models;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Presentation
{
    /// <summary>
    /// Builds the scene object from the data table.
    /// </summary>
    public interface ISceneObjectCatalog
    {
        Task<ModelInstance> Spawn(int id, Transform parent, CancellationToken cancel = default);
    }

    public class SceneObjectCatalog : ISceneObjectCatalog
    {
        private const int NormalType = 0;

        private readonly SceneObjectTable _sceneObjects;
        private readonly IModelStore _models;

        public SceneObjectCatalog(SceneObjectTable sceneObjects, IModelStore models)
        {
            _sceneObjects = sceneObjects ?? throw new ArgumentNullException(nameof(sceneObjects));
            _models = models ?? throw new ArgumentNullException(nameof(models));
        }

        public Task<ModelInstance> Spawn(int id, Transform parent, CancellationToken cancel = default)
        {
            if (!_sceneObjects.TryGetById(id, out var entry)
                || entry.Type != NormalType
                || string.IsNullOrEmpty(entry.ModelPath))
            {
                return Task.FromResult<ModelInstance>(null);
            }

            return _models.Spawn(entry.ModelPath, parent, cancel);
        }
    }
}
