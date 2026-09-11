using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;

namespace Top.Client.Game.Tables
{
    public class TableReader : ITableReader
    {
        private readonly IContentSource _content;
        private readonly TableFormat _format = new TableFormat();

        public TableReader(IContentSource content)
        {
            _content = content;
        }

        public async Task<TableSet> Read(CancellationToken cancellationToken = default)
        {
            var sceneObjects = await ReadTable<SceneObjectEntry>(SceneObjectTable.Path);

            cancellationToken.ThrowIfCancellationRequested();

            var terrains = await ReadTable<TerrainEntry>(TerrainTable.Path);

            cancellationToken.ThrowIfCancellationRequested();

            var maps = await ReadTable<MapEntry>(MapTable.Path);

            return new TableSet(
                new SceneObjectTable(sceneObjects),
                new TerrainTable(terrains),
                new MapTable(maps));
        }

        private async Task<List<TEntry>> ReadTable<TEntry>(string path) where TEntry : TableEntry
        {
            using var stream = new MemoryStream(await _content.Read(path));

            return _format.Read<TEntry>(stream);
        }
    }
}
