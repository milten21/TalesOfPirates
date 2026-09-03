using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;

namespace Top.Client.Game.Tables
{
    public class TableStore : ITableStore
    {
        private readonly IContentSource _content;
        private readonly TableFormat _format = new TableFormat();

        public TableStore(IContentSource content)
        {
            _content = content;
        }

        public async Task<TableSet> Load(CancellationToken cancel = default)
        {
            var sceneObjects = await Read<SceneObjectEntry>(SceneObjectTable.Path);

            cancel.ThrowIfCancellationRequested();

            var terrains = await Read<TerrainEntry>(TerrainTable.Path);

            cancel.ThrowIfCancellationRequested();

            var maps = await Read<MapEntry>(MapTable.Path);

            return new TableSet(
                new SceneObjectTable(sceneObjects),
                new TerrainTable(terrains),
                new MapTable(maps));
        }

        private async Task<List<TEntry>> Read<TEntry>(string path) where TEntry : TableEntry
        {
            using var stream = new MemoryStream(await _content.Read(path));

            return _format.Read<TEntry>(stream);
        }
    }
}
