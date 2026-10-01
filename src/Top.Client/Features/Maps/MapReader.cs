using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content;
using Top.Contracts.Assets.Maps;

namespace Top.Client.Features.Maps;

public class MapReader(IContentSource contentSource)
{
    public async Task<MapGrid> Read(string path, CancellationToken cancellationToken = default)
    {
        var bytes = await contentSource.Read(path);

        cancellationToken.ThrowIfCancellationRequested();

        using var stream = new MemoryStream(bytes);

        return new MapGrid(MapFile.Read(stream));
    }
}
