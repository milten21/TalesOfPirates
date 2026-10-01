using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Ships;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps shipinfo rows to ship entries: the hull a ship shows. What a ship
    /// can be fitted with and what it can do stays with the server.
    /// </summary>
    public class ShipTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "ships";

        public string Path => ShipTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Ships?.Select(ship => new ShipEntry { Id = ship.Id, HullId = ship.HullId });
        }
    }
}
