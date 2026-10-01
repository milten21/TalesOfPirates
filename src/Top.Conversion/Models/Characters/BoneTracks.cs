using System;
using System.Linq;
using System.Numerics;
using Top.Conversion.Models.Gltf;
using Top.Logging;
using Top.Legacy.MindPower.Animation;

namespace Top.Conversion.Models.Characters
{
    /// <summary>
    /// Reads the bone animation tracks of a skeleton as the original plays
    /// them: one local matrix for each bone and frame, in glTF axes.
    /// </summary>
    public static class BoneTracks
    {
        /// <summary>
        /// The keys of every bone. A bone without a usable track has none and
        /// stays in bind pose.
        /// </summary>
        public static Matrix4x4[][] Convert(BoneAnimation skeleton)
        {
            var keys = new Matrix4x4[skeleton.Bones.Length][];

            for (var b = 0; b < skeleton.Bones.Length; b++)
            {
                keys[b] = Keys(skeleton, b);

                if (keys[b] == null)
                {
                    Log.Warning($"bone {skeleton.Bones[b].Name} has no track, it stays in bind pose");
                }
            }

            return keys;
        }

        /// <summary>
        /// The bone's local matrix in each frame, as lwAnimDataBone::GetValue
        /// builds it. A quaternion key goes through the unit-length formula
        /// whatever its length, so a longer key turns further and stretches
        /// across its turn axis. Null when the bone has no track.
        /// </summary>
        private static Matrix4x4[] Keys(BoneAnimation skeleton, int bone)
        {
            var track = skeleton.Tracks != null && bone < skeleton.Tracks.Length ? skeleton.Tracks[bone] : null;

            switch (track)
            {
                case QuaternionBoneTrack quaternion when quaternion.Positions is { Length: > 0 } &&
                                                         quaternion.Rotations is { Length: > 0 }:
                    var keys = new Matrix4x4[quaternion.Rotations.Length];

                    for (var i = 0; i < keys.Length; i++)
                    {
                        var key = Matrix4x4.CreateFromQuaternion(quaternion.Rotations[i]);

                        // Old-format .lab tracks store a single position for non-root bones, that position holds
                        // for every frame.
                        key.Translation = quaternion.Positions[Math.Min(i, quaternion.Positions.Length - 1)];
                        keys[i] = Axis.ToGltf(key);
                    }

                    return keys;
                case MatrixBoneTrack matrix when matrix.Frames is { Length: > 0 }:
                    return matrix.Frames.Select(Axis.ToGltf).ToArray();
                default:
                    return null;
            }
        }
    }
}
