using System.Collections.Generic;

namespace Top.Contracts.Tables.Items
{
    public class ItemTypeTable : Table<ItemTypeEntry>
    {
        public const string Path = "tables/itemtypes.json";

        public ItemTypeTable(IEnumerable<ItemTypeEntry> entries) : base(entries)
        {
        }
    }
}
