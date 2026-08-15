using System.Numerics;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class MapInfoReader
    {
        public static MapInfoRecord Read(TableRow row)
        {
            var record = new MapInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                DisplayName = row.NextString(),
                ShowSwitch = row.NextBool()
            };

            var start = row.NextIntList();

            if (start.Length >= 2)
            {
                record.InitX = start[0];
                record.InitY = start[1];
            }

            row.NextIntList();

            var color = row.NextIntList();

            if (color.Length >= 3)
            {
                record.LightColor = new Vector3(color[0] / 255f, color[1] / 255f, color[2] / 255f);
            }

            return record;
        }
    }
}
