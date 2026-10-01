using Top.Client.Features.Maps;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Features.Characters.Movement;

public class Walkability(MapGrid mapGrid, CharacterControlType controlType, Territory territory)
{
    private const ushort LandRegion = 0x1;

    private const ushort NotFightRegion = 0x2;

    private const ushort BridgeRegion = 0x8;

    private const int CellsPerTile = 2;

    public bool IsWalkable(int cellX, int cellY)
    {
        return !mapGrid.IsBlocked(cellX, cellY)
               && IsMoveAllowed(mapGrid.RegionAt(cellX / CellsPerTile, cellY / CellsPerTile));
    }

    private static bool IsMonster(CharacterControlType controlType)
    {
        return controlType is CharacterControlType.Monster or CharacterControlType.MonsterTree
            or CharacterControlType.MonsterMine or CharacterControlType.MonsterFish
            or CharacterControlType.MonsterDeadBoat or CharacterControlType.MonsterRepairable;
    }

    private bool IsMoveAllowed(ushort region)
    {
        return IsTerritoryAllowed(region) && ((region & NotFightRegion) == 0 || !IsMonster(controlType));
    }

    private bool IsTerritoryAllowed(ushort region)
    {
        return territory switch
        {
            Territory.Any => true,
            Territory.Land => (region & (LandRegion | BridgeRegion)) != 0,
            Territory.Sea => (region & LandRegion) == 0,
            _ => false
        };
    }
}
