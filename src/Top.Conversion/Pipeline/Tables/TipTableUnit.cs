using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps tips lines to tip entries numbered by line.
    /// </summary>
    public class TipTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "tips";

        public string Path => TipTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Tips?.Lines.Select((line, index) => new TipEntry { Id = index + 1, Text = line });
        }
    }
}
