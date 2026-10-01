using System.Collections.Generic;

namespace Top.Contracts.Tables.Items
{
    public class GemTable : Table<GemEntry>
    {
        public const string Path = "tables/gems.json";

        public GemTable(IEnumerable<GemEntry> entries) : base(entries)
        {
        }
    }
}
