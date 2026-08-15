using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class TerrainInfoReader
    {
        public static TerrainInfoRecord Read(TableRow row)
        {
            return new TerrainInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Type = row.NextInt(),
                LeavesFootprints = row.NextBool()
            };
        }
    }
}
