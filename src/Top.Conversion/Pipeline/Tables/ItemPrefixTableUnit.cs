using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Items;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps itempre rows to the name prefixes of equipment. The last two
    /// digits of an item's maximum energy / 10 select a prefix. A row named
    /// "0" is no prefix and emits nothing.
    /// </summary>
    public class ItemPrefixTableUnit(ClientTables tables) : ITableUnit
    {
        private const string NoPrefix = "0";

        public string Name => "itemprefixes";

        public string Path => ItemPrefixTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.ItemPrefixes?
                .Select(prefix => new ItemPrefixEntry { Id = prefix.Id, Name = prefix.Name?.Trim() })
                .Where(prefix => !string.IsNullOrEmpty(prefix.Name) && prefix.Name != NoPrefix);
        }
    }
}
