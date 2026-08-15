using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Top.Content.Packs;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;

namespace Top.Client.Definitions
{
    /// <summary>
    /// Reads the data tables.
    /// </summary>
    public class DefinitionsStore : IDefinitionsStore
    {
        private readonly IComposedContent _content;
        private readonly TableFormat _format = new TableFormat();

        public DefinitionsStore(IComposedContent content)
        {
            _content = content;
        }

        public async Task<DefinitionsDictionary> Load(CancellationToken cancel = default)
        {
            var sceneObjects = await Read<SceneObjectEntry>(SceneObjectTable.Path);

            cancel.ThrowIfCancellationRequested();

            var terrains = await Read<TerrainEntry>(TerrainTable.Path);

            cancel.ThrowIfCancellationRequested();

            var maps = await Read<MapEntry>(MapTable.Path);

            return new DefinitionsDictionary(
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
