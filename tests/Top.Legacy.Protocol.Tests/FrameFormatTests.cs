using System;
using NUnit.Framework;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol.Tests
{
    public class FrameFormatTests
    {
        [Test]
        public void A_length_field_of_another_width_is_refused()
        {
            Assert.That(() => new FrameFormat(lengthSize: 3), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_length_field_before_the_frame_is_refused()
        {
            Assert.That(() => new FrameFormat(lengthOffset: -1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_session_id_of_another_width_is_refused()
        {
            Assert.That(
                () => new FrameFormat(sessionIdSize: 2),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "the session id the gate reads is four bytes wide");
        }
    }
}
