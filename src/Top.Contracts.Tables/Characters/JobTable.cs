using System.Collections.Generic;

namespace Top.Contracts.Tables.Characters
{
    public class JobTable : Table<JobEntry>
    {
        public const string Path = "tables/jobs.json";

        private const int FallbackJob = 0;

        public JobTable(IEnumerable<JobEntry> entries) : base(entries)
        {
        }

        public string NameOf(int job)
        {
            return TryGetById(job, out var entry) || TryGetById(FallbackJob, out entry) ? entry.Name : string.Empty;
        }
    }
}
