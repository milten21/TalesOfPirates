using System.Collections.Generic;

namespace Top.Contracts.Tables.Rigs
{
    public class ActionSetTable : Table<RigEntry>
    {
        public const string Path = "tables/actionsets.json";

        public ActionSetTable(IEnumerable<RigEntry> entries) : base(entries)
        {
        }
    }
}
