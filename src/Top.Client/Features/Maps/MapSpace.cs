using Godot;

namespace Top.Client.Features.Maps;

public static class MapSpace
{
    public static Vector3 ToWorld(float mapX, float mapY, float height)
    {
        return new Vector3(mapX, height, mapY);
    }

    public static Vector2 ToMap(Vector3 world)
    {
        return new Vector2(world.X, world.Z);
    }

    public static float ToWorldYaw(float mapYawDegrees)
    {
        return Mathf.DegToRad(180f - mapYawDegrees);
    }
}
