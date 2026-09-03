using NUnit.Framework;
using Top.Client.Models.Animations;

namespace Top.Client.Models.Tests
{
    public class FlipbookIndexTests
    {
        [Test]
        public void Steps_one_frame_per_animation_frame()
        {
            Assert.That(FlipbookAnimation.FrameIndex(0f, 30f, 8), Is.EqualTo(0));
            Assert.That(FlipbookAnimation.FrameIndex(0.034f, 30f, 8), Is.EqualTo(1));
            Assert.That(FlipbookAnimation.FrameIndex(0.065f, 30f, 8), Is.EqualTo(1));
            Assert.That(FlipbookAnimation.FrameIndex(0.234f, 30f, 8), Is.EqualTo(7));
        }

        [Test]
        public void Wraps_at_the_sequence_end()
        {
            Assert.That(FlipbookAnimation.FrameIndex(8f / 30f, 30f, 8), Is.EqualTo(0));
            Assert.That(FlipbookAnimation.FrameIndex(17.5f / 30f, 30f, 8), Is.EqualTo(1));
        }
    }
}
