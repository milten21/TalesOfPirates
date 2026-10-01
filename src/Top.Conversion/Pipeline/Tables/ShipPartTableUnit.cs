using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Ships;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps the shipiteminfo rows that show on a boat to ship part entries:
    /// the model of a hull, a bow or an engine, and the four propellers an
    /// engine turns. A part with no model and no propeller, such as a cannon
    /// or a component, never changes what a boat shows and is left out.
    /// </summary>
    public class ShipPartTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "shipparts";

        public string Path => ShipPartTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.ShipParts?
                .Where(IsShown)
                .Select(part => new ShipPartEntry
                {
                    Id = part.Id,
                    ModelPath = ClientTables.FindShipModule(part) is { } module
                        ? OutputPaths.ModelContentPath(ContentKind.Machine, module)
                        : null,
                    PropellerIds = part.Motors ?? [],
                });
        }

        private static bool IsShown(ShipItemInfoRecord part)
        {
            return ClientTables.FindShipModule(part) != null || (part.Motors ?? []).Any(propeller => propeller != 0);
        }
    }
}
