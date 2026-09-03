using NUnit.Framework;
using Top.Client.Models.Animations;
using UnityEngine;

namespace Top.Client.Models.Tests
{
    public class OpacitySamplingTests
    {
        private static OpacityAnimationTrack Track(int[] keys, float[] values)
        {
            var track = ScriptableObject.CreateInstance<OpacityAnimationTrack>();
            track.keyFrames = keys;
            track.values = values;

            return track;
        }

        [Test]
        public void Lerps_between_keys()
        {
            var track = Track(new[] { 0, 100 }, new[] { 1f, 0f });

            Assert.That(OpacityAnimation.Sample(track, 25f), Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void Holds_outside_the_key_range()
        {
            var track = Track(new[] { 10, 20 }, new[] { 0.4f, 0.9f });

            Assert.That(OpacityAnimation.Sample(track, 0f), Is.EqualTo(0.4f));
            Assert.That(OpacityAnimation.Sample(track, 25f), Is.EqualTo(0.9f));
        }

        [Test]
        public void Samples_the_hdjd_pulse_shape()
        {
            var track = Track(new[] { 0, 24, 46, 71, 100 },
                new[] { 1f, 0.3f, 1f, 0.2f, 1f });

            Assert.That(OpacityAnimation.Sample(track, 24f), Is.EqualTo(0.3f));
            Assert.That(OpacityAnimation.Sample(track, 35f), Is.EqualTo(Mathf.Lerp(0.3f, 1f, 11f / 22f)).Within(1e-5f));
        }

        [Test]
        public void Loops_one_frame_past_the_last_key()
        {
            var track = Track(new[] { 0, 100 }, new[] { 1f, 0f });

            Assert.That(OpacityAnimation.LoopFrame(track, 106f), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(OpacityAnimation.LoopFrame(track, 100.5f), Is.EqualTo(100.5f).Within(1e-4f));
        }

        [Test]
        public void Duplicate_adjacent_keys_are_tolerated()
        {
            var track = Track(new[] { 0, 50, 50, 100 }, new[] { 1f, 0.2f, 0.8f, 1f });

            Assert.That(OpacityAnimation.Sample(track, 50f), Is.EqualTo(0.2f));
            Assert.That(OpacityAnimation.Sample(track, 60f), Is.EqualTo(Mathf.Lerp(0.8f, 1f, 0.2f)).Within(1e-5f));
        }
    }
}
