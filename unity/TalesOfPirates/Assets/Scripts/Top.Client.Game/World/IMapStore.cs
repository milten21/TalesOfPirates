using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Top.Client.Game.World
{
    /// <summary>
    /// Reads converted maps and the materials that draw them.
    /// </summary>
    public interface IMapStore
    {
        Task<MapData> Load(string path, CancellationToken cancel = default);

        Task<Material> GetTerrainMaterial(CancellationToken cancel = default);

        Task<Material> GetWaterMaterial(CancellationToken cancel = default);
    }
}
