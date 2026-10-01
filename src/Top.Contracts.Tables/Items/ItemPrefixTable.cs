using System.Collections.Generic;

namespace Top.Contracts.Tables.Items
{
    public class ItemPrefixTable : Table<ItemPrefixEntry>
    {
        public const string Path = "tables/itemprefixes.json";

        public ItemPrefixTable(IEnumerable<ItemPrefixEntry> entries) : base(entries)
        {
        }
    }
}
