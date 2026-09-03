using NUnit.Framework;
using Top.Client.Models.Animations;
using Top.Contracts.Assets.Models;
using Top.Contracts.Assets.Models.Extras;
using UnityEngine;

namespace Top.Client.Models.Tests
{
    public class TrackMapperTests
    {
        [Test]
        public void Uv_frames_keep_the_six_cells_that_reach_a_coordinate()
        {
            var track = TrackMapper.CreateUvTrack(new UvAnimationExtras
            {
                Frames = new[] { new[] { 2f, 3f, 4f, 5f, 6f, 7f } },
            });

            var matrix = track.frames[0];

            Assert.That(track.framesPerSecond, Is.EqualTo(AnimationRate.FramesPerSecond));
            Assert.That(new[] { matrix.m00, matrix.m01, matrix.m10, matrix.m11, matrix.m20, matrix.m21 },
                Is.EqualTo(new[] { 2f, 3f, 4f, 5f, 6f, 7f }));
            Assert.That(matrix.m22, Is.EqualTo(1f), "the cells left out stay identity");
            Assert.That(matrix.m33, Is.EqualTo(1f));

            Object.DestroyImmediate(track);
        }

        [Test]
        public void A_uv_frame_the_file_cut_short_stays_identity()
        {
            var track = TrackMapper.CreateUvTrack(new UvAnimationExtras
            {
                Frames = new[] { new[] { 2f, 3f } },
            });

            Assert.That(track.frames[0], Is.EqualTo(Matrix4x4.identity));

            Object.DestroyImmediate(track);
        }

        [Test]
        public void Opacity_keys_carry_over_in_parallel()
        {
            var track = TrackMapper.CreateOpacityTrack(new OpacityAnimationExtras
            {
                FramesPerSecond = 15f,
                KeyFrames = new[] { 0, 12, 30 },
                Values = new[] { 1f, 0.5f, 0f },
            });

            Assert.That(track.framesPerSecond, Is.EqualTo(15f));
            Assert.That(track.keyFrames, Is.EqualTo(new[] { 0, 12, 30 }));
            Assert.That(track.values, Is.EqualTo(new[] { 1f, 0.5f, 0f }));

            Object.DestroyImmediate(track);
        }

        [Test]
        public void Flipbook_frames_resolve_through_the_caller_and_a_hole_stays_empty()
        {
            var red = new Texture2D(1, 1);
            var track = TrackMapper.CreateFlipbookTrack(
                new FlipbookExtras { Frames = new[] { 4, FlipbookExtras.NoTexture } },
                index => index == 4 ? red : null);

            Assert.That(track.frames, Is.EqualTo(new[] { red, null }));

            Object.DestroyImmediate(track);
            Object.DestroyImmediate(red);
        }

        [Test]
        public void A_section_without_frames_maps_to_an_empty_track()
        {
            var uv = TrackMapper.CreateUvTrack(new UvAnimationExtras());
            var opacity = TrackMapper.CreateOpacityTrack(new OpacityAnimationExtras());
            var flipbook = TrackMapper.CreateFlipbookTrack(new FlipbookExtras(), _ => null);

            Assert.That(uv.frames, Is.Empty);
            Assert.That(opacity.keyFrames, Is.Empty);
            Assert.That(opacity.values, Is.Empty);
            Assert.That(flipbook.frames, Is.Empty);

            Object.DestroyImmediate(uv);
            Object.DestroyImmediate(opacity);
            Object.DestroyImmediate(flipbook);
        }
    }
}
