using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class TipTable : Table<TipEntry>
    {
        public const string Path = "tables/tips.json";

        public TipTable(IEnumerable<TipEntry> entries) : base(entries)
        {
        }
    }
}
