using System.Collections.Generic;
using System.Numerics;

namespace Top.Client.Tools.Debug.ServerPaths;

public record MoveStep(IReadOnlyList<Vector2> Points, long Timestamp);
