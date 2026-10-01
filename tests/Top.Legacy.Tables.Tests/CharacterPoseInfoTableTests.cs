using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class CharacterPoseInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/characterposeinfo.txt"));
            var table = TableFile.Read<CharacterPoseInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Normal Wait"));
            Assert.That(record.RealPoseIds, Is.EqualTo(new[] { 1, 55, 109, 163, 217, 271, 325 }));
        }

        [Test]
        public void Pose_grid_is_regular_across_every_action_and_stance()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/characterposeinfo.txt"));
            var table = TableFile.Read<CharacterPoseInfoRecord>(stream);

            Assert.That(table.Count, Is.EqualTo(54));

            foreach (var record in table)
            {
                Assert.That(record.RealPoseIds.Length, Is.EqualTo(7));

                for (var stance = 0; stance < 7; stance++)
                {
                    Assert.That(record.RealPoseIds[stance], Is.EqualTo(record.Id + 54 * stance),
                        $"action {record.Id}, stance {stance}");
                }
            }
        }
    }
}
