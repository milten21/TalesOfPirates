using System.Collections.Generic;

namespace Top.Client.Features.Maps;

public class MapNotFoundException(string message) : KeyNotFoundException(message)
{
}
