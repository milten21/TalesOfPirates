using System.Linq;
using Top.Content;
using Top.Contracts.Tables.World;

namespace Top.Client.Features.Maps;

public class MapLookup(MapTable maps, IContentSource contentSource)
{
    public MapEntry FindByName(string name)
    {
        var map = maps.FirstOrDefault(entry => entry.Name == name)
                  ?? throw new MapNotFoundException($"the map table names no map called {name}");

        return RequireFile(map);
    }

    public MapEntry FindById(int id)
    {
        if (!maps.TryGetById(id, out var map))
        {
            throw new MapNotFoundException($"the map table has no map {id}");
        }

        return RequireFile(map);
    }

    private MapEntry RequireFile(MapEntry map)
    {
        return contentSource.Exists(map.MapPath) ? map : throw new MapFileNotFoundException(map);
    }
}
