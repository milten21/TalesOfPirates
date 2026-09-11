using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Top.Client.Models
{
    public interface IModelStore
    {
        Task<ModelInstance> Instantiate(string path, Transform parent, CancellationToken cancellationToken = default);
        Task EnsureLoaded(IEnumerable<string> paths);
    }
}
