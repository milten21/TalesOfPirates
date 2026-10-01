using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Features.Characters.Movement;

public class GroundPicker(MapGrid mapGrid, Node3D worldRoot, float reach = 70f, float step = 0.6f,
    float sideShift = 0.3f)
{
    public bool TryPick(Camera3D camera, Vector2 onViewport, out Vector2 mapPoint)
    {
        var toMap = worldRoot.GlobalTransform.AffineInverse();
        var origin = toMap * camera.ProjectRayOrigin(onViewport);
        var direction = (toMap.Basis * camera.ProjectRayNormal(onViewport)).Normalized();

        return TryPick(origin, direction, out mapPoint);
    }

    private bool TryPick(Vector3 origin, Vector3 direction, out Vector2 mapPoint)
    {
        var start = MapSpace.ToMap(origin);
        var span = MapSpace.ToMap(origin + (direction * reach)) - start;
        var along = span.Normalized();
        var across = new Vector2(along.Y, -along.X) * sideShift;
        var sampleCount = (int)(span.Length() / step);

        for (var sample = 0; sample < sampleCount; sample++)
        {
            var centre = start + (along * (sample * step));

            if (TryHitTile(centre + across, origin, direction, out mapPoint)
                || TryHitTile(centre - across, origin, direction, out mapPoint))
            {
                return true;
            }
        }

        mapPoint = default;

        return false;
    }

    private bool TryHitTile(Vector2 sample, Vector3 origin, Vector3 direction, out Vector2 mapPoint)
    {
        mapPoint = default;

        var tileX = Mathf.FloorToInt(sample.X);
        var tileY = Mathf.FloorToInt(sample.Y);

        if (tileX < 0 || tileY < 0 || tileX >= mapGrid.TileCountX || tileY >= mapGrid.TileCountY)
        {
            return false;
        }

        var cell00 = mapGrid.GroundPointAt(new Vector2(tileX, tileY));
        var cell10 = mapGrid.GroundPointAt(new Vector2(tileX + 1, tileY));
        var cell01 = mapGrid.GroundPointAt(new Vector2(tileX, tileY + 1));
        var cell11 = mapGrid.GroundPointAt(new Vector2(tileX + 1, tileY + 1));

        var hit = Geometry3D.RayIntersectsTriangle(origin, direction, cell00, cell10, cell01);

        if (hit.VariantType == Variant.Type.Nil)
        {
            hit = Geometry3D.RayIntersectsTriangle(origin, direction, cell01, cell10, cell11);
        }

        if (hit.VariantType == Variant.Type.Nil)
        {
            return false;
        }

        mapPoint = MapSpace.ToMap(hit.AsVector3());

        return true;
    }
}
