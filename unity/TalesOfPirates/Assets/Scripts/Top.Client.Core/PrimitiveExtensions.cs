using Top.Contracts;
using UnityEngine;

namespace Top.Client.Core
{
    public static class PrimitiveExtensions
    {
        public static Vector3 ToUnity(this Float3 value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        public static Color32 ToUnity(this Rgb color)
        {
            return new Color32(color.R, color.G, color.B, byte.MaxValue);
        }
    }
}
