using System;
using NUnit.Framework;

namespace Top.Legacy.Protocol.Tests
{
    public class GateSettingsTests
    {
        [Test]
        public void The_defaults_are_the_intervals_the_spec_fixes()
        {
            var settings = new GateSettings();

            Assert.That(settings.IdleInterval, Is.EqualTo(TimeSpan.FromSeconds(25)));
            Assert.That(settings.ReadTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(settings.ClientVersion, Is.EqualTo(32125), "[Main] Version of GateServer.cfg");
            Assert.That(settings.IsEncrypted, Is.True);
        }

        [Test]
        public void An_idle_interval_of_zero_is_refused()
        {
            Assert.That(
                () => new GateSettings(idleInterval: TimeSpan.Zero),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_read_timeout_of_zero_is_refused()
        {
            Assert.That(
                () => new GateSettings(readTimeout: TimeSpan.Zero),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void An_interval_past_a_count_of_milliseconds_is_refused()
        {
            Assert.That(
                () => new GateSettings(idleInterval: TimeSpan.FromDays(30)),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
