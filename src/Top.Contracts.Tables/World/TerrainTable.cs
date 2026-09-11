using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class TerrainTable : Table<TerrainEntry>
    {
        public const string Path = "tables/terrains.json";

        public TerrainTable(IEnumerable<TerrainEntry> entries) : base(entries)
        {
        }
    }
}
