using System.Threading;
using System.Threading.Tasks;

namespace Top.Client.Game.World
{
    public interface IMapReader
    {
        Task<MapData> Read(string path, CancellationToken cancellationToken = default);
    }
}
