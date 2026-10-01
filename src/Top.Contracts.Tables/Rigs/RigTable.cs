using System.Collections.Generic;

namespace Top.Contracts.Tables.Rigs
{
    public class RigTable : Table<RigEntry>
    {
        public const string Path = "tables/rigs.json";

        public RigTable(IEnumerable<RigEntry> entries) : base(entries)
        {
        }
    }
}
