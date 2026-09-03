using System.Collections.Generic;
using System.Linq;
using Top.Contracts;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps mapinfo rows to map entries.
    /// </summary>
    public class MapTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "maps";

        public string Path => MapTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.Maps?.Select(Entry);
        }

        private static MapEntry Entry(MapInfoRecord row)
        {
            return new MapEntry
            {
                Id = row.Id,
                Name = row.Name,
                MapPath = !string.IsNullOrEmpty(row.Name)
                    ? OutputPaths.MapContentPath(row.Name)
                    : null,
                DisplayName = string.IsNullOrEmpty(row.DisplayName) ? null : row.DisplayName,
                ShowsAreaNames = row.ShowSwitch,
                StartX = row.InitX,
                StartY = row.InitY,
                LightDirection = new Float3(row.LightDirection.X, row.LightDirection.Y, row.LightDirection.Z),
                LightColor = TableColor.Read(row.LightColor),
            };
        }
    }
}
