using System.Collections.Generic;

namespace Top.Contracts.Tables.Ships
{
    public class ShipTable : Table<ShipEntry>
    {
        public const string Path = "tables/ships.json";

        public ShipTable(IEnumerable<ShipEntry> entries) : base(entries)
        {
        }
    }
}
