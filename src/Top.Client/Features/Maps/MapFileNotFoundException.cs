using System.IO;
using Top.Contracts.Tables.World;

namespace Top.Client.Features.Maps;

public class MapFileNotFoundException(MapEntry map)
    : FileNotFoundException($"map {map.Name} has no file at {map.MapPath}", map.MapPath)
{
    public MapEntry Map { get; } = map;
}
