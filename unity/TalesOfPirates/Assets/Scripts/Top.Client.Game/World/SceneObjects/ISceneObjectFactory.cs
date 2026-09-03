using System.Threading;
using System.Threading.Tasks;
using Top.Client.Models;
using UnityEngine;

namespace Top.Client.Game.World.SceneObjects
{
    public interface ISceneObjectFactory
    {
        Task<ModelInstance> Build(int id, Transform parent, CancellationToken cancel = default);
    }
}
