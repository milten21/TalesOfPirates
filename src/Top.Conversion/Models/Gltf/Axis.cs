using System.Numerics;
using Top.Conversion.Gltf;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// The axis change from the original client's Z-up left-handed space to the
    /// Y-up right-handed space glTF expects.
    /// </summary>
    public static class Axis
    {
        public static Vector3 ToGltf(Vector3 v)
        {
            return new Vector3(v.X, v.Z, v.Y);
        }

        public static Matrix4x4 ToGltf(Matrix4x4 m)
        {
            return new Matrix4x4(
                m.M11, m.M13, m.M12, m.M14,
                m.M31, m.M33, m.M32, m.M34,
                m.M21, m.M23, m.M22, m.M24,
                m.M41, m.M43, m.M42, m.M44);
        }

        public static float[] ToGltfMatrix(Matrix4x4 m)
        {
            return new[]
            {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };
        }

        public static void Place(GltfNodeRef node, Matrix4x4 local)
        {
            if (!local.IsIdentity)
            {
                node.WithMatrix(ToGltfMatrix(ToGltf(local)));
            }
        }
    }
}
