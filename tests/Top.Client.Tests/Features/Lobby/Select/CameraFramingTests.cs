using NUnit.Framework;
using Top.Client.Core;
using Top.Client.Features.Lobby.Select;

namespace Top.Client.Tests.Features.Lobby.Select
{
    public class CameraFramingTests
    {
        private const float OriginalVertical = 70f;

        private const float OriginalHorizontal = 86.06f;

        [Test]
        public void The_original_aspect_needs_no_move()
        {
            Assert.That(
                CameraFraming.MeasurePullBack(OriginalHorizontal, 4f / 3f, OriginalVertical, 4.4f),
                Is.EqualTo(0f).Within(1e-3f),
                "a 4:3 window already shows what the original showed");
        }

        [Test]
        public void A_wide_window_moves_the_eye_back()
        {
            var pullBack = CameraFraming.MeasurePullBack(
                OriginalHorizontal, 16f / 9f, OriginalVertical, 4.4f);

            // 70 vertical at 4:3 is 86.06 horizontal, which at 16:9 leaves 55.4
            // vertical. tan(35) / tan(27.7) is 4 / 3, so the distance grows by a
            // third.
            Assert.That(pullBack, Is.EqualTo(4.4f / 3f).Within(1e-2f));
        }

        [Test]
        public void A_taller_window_than_the_original_needs_no_move()
        {
            Assert.That(
                CameraFraming.MeasurePullBack(OriginalHorizontal, 1f, OriginalVertical, 4.4f),
                Is.EqualTo(0f),
                "a narrow window already sees more than the original vertically");
        }

        [Test]
        public void A_wider_window_moves_the_eye_further()
        {
            var wide = CameraFraming.MeasurePullBack(
                OriginalHorizontal, 16f / 9f, OriginalVertical, 4.4f);
            var wider = CameraFraming.MeasurePullBack(
                OriginalHorizontal, 21f / 9f, OriginalVertical, 4.4f);

            Assert.That(wider, Is.GreaterThan(wide));
        }

        [Test]
        public void The_move_is_proportional_to_the_depth_it_frames()
        {
            var near = CameraFraming.MeasurePullBack(OriginalHorizontal, 16f / 9f, OriginalVertical, 4f);
            var far = CameraFraming.MeasurePullBack(OriginalHorizontal, 16f / 9f, OriginalVertical, 8f);

            Assert.That(far, Is.EqualTo(near * 2f).Within(1e-3f));
        }

        [TestCase(0f, 4.4f)]
        [TestCase(-1f, 4.4f)]
        [TestCase(1.78f, 0f)]
        [TestCase(1.78f, -1f)]
        public void A_nonsensical_viewport_or_depth_moves_nothing(float aspect, float depth)
        {
            Assert.That(
                CameraFraming.MeasurePullBack(OriginalHorizontal, aspect, OriginalVertical, depth),
                Is.EqualTo(0f));
        }

        [TestCase(0f)]
        [TestCase(180f)]
        public void A_nonsensical_angle_moves_nothing(float degrees)
        {
            Assert.That(
                CameraFraming.MeasurePullBack(degrees, 16f / 9f, OriginalVertical, 4.4f),
                Is.EqualTo(0f));
            Assert.That(
                CameraFraming.MeasurePullBack(OriginalHorizontal, 16f / 9f, degrees, 4.4f),
                Is.EqualTo(0f));
        }
    }
}
