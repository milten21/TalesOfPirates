using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class CharacterActionTableTests
    {
        private static CharacterActionTable ReadFixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/CharacterAction.tx"));
            return CharacterActionTable.Read(stream);
        }

        private static CharacterActionTable ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return CharacterActionTable.Read(stream);
        }

        [Test]
        public void Parses_type_1_actions()
        {
            var table = ReadFixture();

            Assert.That(table.TryGetActions(1, out var actions), Is.True);

            var waiting = actions.Single(a => a.ActionNo == 1);
            Assert.That(waiting.StartFrame, Is.EqualTo(2));
            Assert.That(waiting.EndFrame, Is.EqualTo(59));

            var run = actions.Single(a => a.ActionNo == 5);
            Assert.That(run.StartFrame, Is.EqualTo(1921));
            Assert.That(run.EndFrame, Is.EqualTo(1937));
            Assert.That(run.KeyFrames, Is.EqualTo(new[] { 1921, 1929 }));
        }

        [Test]
        public void Zero_ranges_are_dropped_at_lookup()
        {
            var table = ReadFixture();

            table.TryGetActions(1, out var actions);

            Assert.That(actions.Any(a => a.ActionNo == 14), Is.False);
        }

        [Test]
        public void Missing_type_returns_false()
        {
            var table = ReadFixture();

            Assert.That(table.TryGetActions(724, out _), Is.False);
        }

        [Test]
        public void Tolerates_a_run_of_spaces_as_a_field_separator()
        {
            var table = ReadFixture();

            table.TryGetActions(2, out var actions);

            var mining = actions.Single(a => a.ActionNo == 28);
            Assert.That(mining.StartFrame, Is.EqualTo(2292));
            Assert.That(mining.EndFrame, Is.EqualTo(2320));
            Assert.That(mining.KeyFrames, Is.EqualTo(new[] { 2304 }));
        }

        [TestCase(1, 245)]
        [TestCase(2, 138)]
        [TestCase(3, 241)]
        [TestCase(4, 101)]
        public void Non_zero_action_counts_match_the_shipped_data(int characterType, int expectedCount)
        {
            var table = ReadFixture();

            Assert.That(table.TryGetActions(characterType, out var actions), Is.True);
            Assert.That(actions.Length, Is.EqualTo(expectedCount));
        }

        [Test]
        public void Comments_and_blank_lines_are_skipped()
        {
            var table = ReadText("// header\n\n7\n\t1\t10\t20\n");

            Assert.That(table.TryGetActions(7, out var actions), Is.True);
            Assert.That(actions.Single().EndFrame, Is.EqualTo(20));
        }

        [Test]
        public void Duplicate_action_rows_keep_the_last()
        {
            var table = ReadText("7\n\t1\t10\t20\n\t1\t30\t40\n");

            table.TryGetActions(7, out var actions);

            Assert.That(actions.Single().StartFrame, Is.EqualTo(30));
        }

        [Test]
        public void Repeated_type_header_replaces_the_earlier_block()
        {
            var table = ReadText("7\n\t1\t10\t20\n7\n\t2\t30\t40\n");

            table.TryGetActions(7, out var actions);

            Assert.That(actions.Single().ActionNo, Is.EqualTo(2));
        }

        [Test]
        public void Malformed_rows_are_skipped()
        {
            var table = ReadText("7\n\tbroken row here\n\t2\t10\t20\n");

            table.TryGetActions(7, out var actions);

            Assert.That(actions.Single().ActionNo, Is.EqualTo(2));
        }

        [Test]
        public void Separator_only_lines_are_skipped()
        {
            var table = ReadText(" ,\n7\n\t1\t10\t20\n");

            Assert.That(table.TryGetActions(7, out _), Is.True);
        }
    }
}
