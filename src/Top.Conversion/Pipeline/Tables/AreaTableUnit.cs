using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps areaset rows to area entries.
    /// </summary>
    public class AreaTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "areas";

        public string Path => AreaTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Areas?.Select(Entry);
        }

        private static AreaEntry Entry(AreaRecord row)
        {
            return new AreaEntry
            {
                Id = row.Id,
                Name = string.IsNullOrEmpty(row.Name) ? null : row.Name,
                IsCity = row.IsCity,
            };
        }
    }
}
