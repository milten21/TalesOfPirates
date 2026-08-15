using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps terraininfo rows to terrain entries, naming each texture where it
    /// lands in the tree.
    /// </summary>
    public class TerrainTableUnit(ClientTables tables) : ITableUnit
    {
        public const string TextureKind = "terrain";

        public string Name => "terrains";

        public string Path => TerrainTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Terrain?.Select(Entry);
        }

        private static TerrainEntry Entry(TerrainInfoRecord row)
        {
            return new TerrainEntry
            {
                Id = row.Id,
                TexturePath = !string.IsNullOrEmpty(row.Name)
                    ? OutputPaths.TextureContentPath(TextureKind, row.Name)
                    : null,
                Type = row.Type,
                LeavesFootprints = row.LeavesFootprints,
            };
        }
    }
}
