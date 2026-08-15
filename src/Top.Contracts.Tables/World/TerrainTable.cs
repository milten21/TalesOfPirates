using System.Collections.Generic;
using System.Linq;

namespace Top.Contracts.Tables.World
{
    public class TerrainTable : Table<TerrainEntry>
    {
        public const string Path = "tables/terrains.json";

        public TerrainTable(IEnumerable<TerrainEntry> entries) : base(entries)
        {
        }

        public string[] TexturePaths()
        {
            var last = this.Select(entry => entry.Id).Prepend(0).Max();

            var paths = new string[last + 1];

            foreach (var entry in this)
            {
                if (entry.Id >= 0)
                {
                    paths[entry.Id] = entry.TexturePath;
                }
            }

            return paths;
        }
    }
}
