using System;
using Top.Contracts.Assets.Models.Extras;
using UnityEngine;

namespace Top.Client.Models.Animations
{
    /// <summary>
    /// Turns the animation sections a material carries into the tracks the
    /// components play. The numbers are the contract's; what happens here is
    /// the step that needs Unity.
    /// </summary>
    public static class TrackMapper
    {
        public static UvAnimationTrack CreateUvTrack(UvAnimationExtras extras)
        {
            var frames = extras.Frames ?? Array.Empty<float[]>();
            var track = ScriptableObject.CreateInstance<UvAnimationTrack>();

            track.framesPerSecond = extras.FramesPerSecond;
            track.frames = new Matrix4x4[frames.Length];

            for (var i = 0; i < frames.Length; i++)
            {
                track.frames[i] = ToMatrix(frames[i]);
            }

            return track;
        }

        public static OpacityAnimationTrack CreateOpacityTrack(OpacityAnimationExtras extras)
        {
            var track = ScriptableObject.CreateInstance<OpacityAnimationTrack>();

            track.framesPerSecond = extras.FramesPerSecond;
            track.keyFrames = extras.KeyFrames ?? Array.Empty<int>();
            track.values = extras.Values ?? Array.Empty<float>();

            return track;
        }

        public static FlipbookTrack CreateFlipbookTrack(FlipbookExtras extras,
            Func<int, Texture2D> resolveTexture)
        {
            var frames = extras.Frames ?? Array.Empty<int>();
            var track = ScriptableObject.CreateInstance<FlipbookTrack>();

            track.framesPerSecond = extras.FramesPerSecond;
            track.frames = new Texture2D[frames.Length];

            for (var i = 0; i < frames.Length; i++)
            {
                track.frames[i] = frames[i] != FlipbookExtras.NoTexture
                    ? resolveTexture(frames[i])
                    : null;
            }

            return track;
        }

        private static Matrix4x4 ToMatrix(float[] frame)
        {
            var matrix = Matrix4x4.identity;

            if (frame == null || frame.Length < 6)
            {
                return matrix;
            }

            matrix.m00 = frame[0];
            matrix.m01 = frame[1];
            matrix.m10 = frame[2];
            matrix.m11 = frame[3];
            matrix.m20 = frame[4];
            matrix.m21 = frame[5];

            return matrix;
        }
    }
}
