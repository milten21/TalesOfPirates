using System.Collections.Generic;

namespace Top.Contracts.Tables.Items
{
    public class ItemTable : Table<ItemEntry>
    {
        public const string Path = "tables/items.json";

        public ItemTable(IEnumerable<ItemEntry> entries) : base(entries)
        {
        }
    }
}
