using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Game.World
{
    public class MapReader : IMapReader
    {
        private readonly IContentSource _contentSource;

        public MapReader(IContentSource contentSource)
        {
            _contentSource = contentSource;
        }

        public async Task<MapData> Read(string path, CancellationToken cancellationToken = default)
        {
            var bytes = await _contentSource.Read(path);

            cancellationToken.ThrowIfCancellationRequested();

            using var stream = new MemoryStream(bytes);

            return new MapData(MapFile.Read(stream));
        }
    }
}
