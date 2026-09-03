using UnityEngine;

namespace Top.Client.Game.World
{
    /// <summary>
    /// The pinned axis mapping: map x to -X, map y to Z, height to Y. The x flip
    /// keeps the original left-handed z-up world unmirrored in Unity.
    /// </summary>
    public static class MapSpace
    {
        public static Vector3 ToWorld(float mapX, float mapY, float height)
        {
            return new Vector3(-mapX, height, mapY);
        }

        public static Vector2 ToMap(Vector3 world)
        {
            return new Vector2(-world.x, world.z);
        }
    }
}
