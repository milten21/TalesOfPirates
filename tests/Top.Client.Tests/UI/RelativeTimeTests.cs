using System;
using NUnit.Framework;
using Top.Client.UI;

namespace Top.Client.Tests.UI
{
    public class RelativeTimeTests
    {
        private static readonly DateTime Now = new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Local);

        [TestCase(0, "today")]
        [TestCase(1, "yesterday")]
        [TestCase(2, "2 days ago")]
        [TestCase(6, "6 days ago")]
        [TestCase(7, "last week")]
        [TestCase(13, "last week")]
        [TestCase(14, "2 weeks ago")]
        [TestCase(20, "2 weeks ago")]
        [TestCase(21, "3 weeks ago")]
        [TestCase(29, "4 weeks ago")]
        [TestCase(30, "last month")]
        [TestCase(59, "last month")]
        [TestCase(60, "2 months ago")]
        [TestCase(89, "2 months ago")]
        [TestCase(90, "3 months ago")]
        [TestCase(400, "13 months ago")]
        public void A_time_some_days_back_reads_as_its_text(int days, string text)
        {
            Assert.That(RelativeTime.Describe(Now.AddDays(-days), Now), Is.EqualTo(text));
        }

        [Test]
        public void A_utc_time_counts_its_days_in_local_time()
        {
            Assert.That(RelativeTime.Describe(Now.AddDays(-2).ToUniversalTime(), Now), Is.EqualTo("2 days ago"));
        }

        [Test]
        public void A_time_early_in_the_day_is_today_late_in_the_same_day()
        {
            DateTime lateNow = Now.Date.AddHours(23).AddMinutes(59);

            Assert.That(RelativeTime.Describe(Now.Date.AddMinutes(1), lateNow), Is.EqualTo("today"));
        }

        [Test]
        public void A_time_late_in_the_day_before_is_yesterday_early_in_the_next_day()
        {
            DateTime earlyNow = Now.Date.AddMinutes(1);

            Assert.That(RelativeTime.Describe(Now.Date.AddMinutes(-1), earlyNow), Is.EqualTo("yesterday"));
        }

        [Test]
        public void A_time_after_now_reads_as_today()
        {
            Assert.That(RelativeTime.Describe(Now.AddDays(3), Now), Is.EqualTo("today"));
        }
    }
}
