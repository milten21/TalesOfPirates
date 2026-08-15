using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class MapTable : Table<MapEntry>
    {
        public const string Path = "tables/maps.json";

        public MapTable(IEnumerable<MapEntry> entries) : base(entries)
        {
        }
    }
}
