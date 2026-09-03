using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Top.Client.Models
{
    public interface IModelStore
    {
        Task<ModelInstance> Spawn(string path, Transform parent, CancellationToken cancel = default);
        Task Preload(IEnumerable<string> paths);
    }
}
