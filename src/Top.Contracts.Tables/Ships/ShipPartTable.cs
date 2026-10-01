using System.Collections.Generic;

namespace Top.Contracts.Tables.Ships
{
    public class ShipPartTable : Table<ShipPartEntry>
    {
        public const string Path = "tables/shipparts.json";

        public ShipPartTable(IEnumerable<ShipPartEntry> entries) : base(entries)
        {
        }
    }
}
