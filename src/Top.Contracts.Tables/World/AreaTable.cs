using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class AreaTable : Table<AreaEntry>
    {
        public const string Path = "tables/areas.json";

        public AreaTable(IEnumerable<AreaEntry> entries) : base(entries)
        {
        }
    }
}
