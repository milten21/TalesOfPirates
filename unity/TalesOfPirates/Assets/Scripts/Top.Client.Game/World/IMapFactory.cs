using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Top.Client.Game.World
{
    public interface IMapFactory
    {
        Task<MapInstance> Instantiate(int mapId, Transform parent, CancellationToken cancellationToken = default);
    }
}
