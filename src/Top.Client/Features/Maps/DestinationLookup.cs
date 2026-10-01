using System;
using Godot;
using Top.Contracts.Tables.World;

namespace Top.Client.Features.Maps;

public class DestinationLookup(MapEntry map, AreaTable areas, MapGrid mapGrid)
{
    private const byte NoArea = 0;

    private const ushort NotFightRegion = 0x2;

    private const ushort PlayerKillingRegion = 0x4;

    public Destination FindAt(Vector2 mapPoint)
    {
        var tileX = (int)Math.Floor(mapPoint.X);
        var tileY = (int)Math.Floor(mapPoint.Y);
        var area = FindArea(mapGrid.IslandAt(tileX, tileY));

        return new Destination(area?.Name ?? map.DisplayName ?? map.Name, ToZoneType(area),
            ToZoneRule(mapGrid.RegionAt(tileX, tileY)));
    }

    private static ZoneType? ToZoneType(AreaEntry? area)
    {
        return area switch
        {
            null => null,
            { IsCity: true } => ZoneType.City,
            _ => ZoneType.Field
        };
    }

    private static ZoneRule ToZoneRule(ushort region)
    {
        if ((region & NotFightRegion) != 0)
        {
            return ZoneRule.Safe;
        }

        return (region & PlayerKillingRegion) != 0 ? ZoneRule.PlayerKilling : ZoneRule.Battle;
    }

    private AreaEntry? FindArea(byte areaId)
    {
        return areaId != NoArea && areas.TryGetById(areaId, out var area) ? area : null;
    }
}
