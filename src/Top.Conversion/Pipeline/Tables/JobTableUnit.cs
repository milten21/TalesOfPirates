using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Characters;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Emits the job names by job index. The original keeps them in code
    /// (g_szJobName in CommFunc.cpp), not in a data file.
    /// </summary>
    public class JobTableUnit : ITableUnit
    {
        private static readonly string[] Names =
        [
            "Newbie", "Swordsman", "Hunter", "Sailor", "Explorer", "Herbalist", "Engineer", "Merchant", "Champion",
            "Crusader", "White Knight", "Animal Tamer", "Sharpshooter", "Cleric", "Seal master", "Captain", "Voyager",
            "Upstart", "Engineer",
        ];

        public string Name => "jobs";

        public string Path => JobTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return Names.Select((name, index) => new JobEntry { Id = index, Name = name });
        }
    }
}
