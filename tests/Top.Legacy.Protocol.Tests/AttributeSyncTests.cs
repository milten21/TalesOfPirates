using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class AttributeSyncTests
    {
        private const string PinnedSpeedRaisedHex =
            "00000002" + "04" + "0002" + "2C" + "00000258" + "45" + "00000258";

        private const string PinnedLevelEightyHex =
            "0000000B" + "04" + "0012" +
            "00" + "00000050" + "01" + "00000843" + "02" + "00000104" + "06" + "000000FA" + "07" + "00000071" +
            "0F" + "EE6B2800" + "10" + "F8F387A5" + "11" + "E9E87690" + "1F" + "00000843" + "20" + "00000104" +
            "24" + "000000A8" + "25" + "000000A8" + "28" + "00000019" + "38" + "00000843" + "39" + "00000104" +
            "3D" + "000000A8" + "3E" + "000000A8" + "41" + "00000019";

        private const string PinnedSwordsmanHex =
            "0000000B" + "04" + "0007" +
            "04" + "00000001" + "1F" + "0000008C" + "23" + "00000005" + "28" + "00000003" + "38" + "0000008C" +
            "3C" + "00000005" + "41" + "00000003";

        private const byte TaskSync = 4;

        private const int Swordsman = 1;

        private const byte BaseMoveSpeed = 69;

        [Test]
        public void A_pinned_sync_names_the_character_the_sync_type_and_the_changed_attributes()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedSpeedRaisedHex));

            var sync = AttributeSync.Read(packet);

            Assert.That(sync.WorldId, Is.EqualTo(2));
            Assert.That(sync.AttributeType, Is.EqualTo(TaskSync));
            Assert.That(sync.Attributes.Select(attribute => (attribute.Id, attribute.Value)),
                Is.EqualTo(new[] { (CharacterAttribute.MoveSpeed, 600L), (BaseMoveSpeed, 600L) }));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_level_up_reads_the_vitals_the_job_and_the_experience()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedLevelEightyHex));

            var sync = AttributeSync.Read(packet);
            var values = sync.Attributes.ToDictionary(attribute => attribute.Id, attribute => attribute.Value);

            Assert.That(sync.WorldId, Is.EqualTo(11));
            Assert.That(values[CharacterAttribute.Level], Is.EqualTo(80));
            Assert.That(values[CharacterAttribute.Health], Is.EqualTo(2115));
            Assert.That(values[CharacterAttribute.MaxHealth], Is.EqualTo(2115));
            Assert.That(values[CharacterAttribute.Mana], Is.EqualTo(260));
            Assert.That(values[CharacterAttribute.MaxMana], Is.EqualTo(260));
            Assert.That(values, Does.Not.ContainKey(CharacterAttribute.Job), "a level-up keeps the job");
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_level_up_reads_experience_and_both_thresholds_as_unsigned()
        {
            var sync = AttributeSync.Read(new PacketReader(Hex.Bytes(PinnedLevelEightyHex)));
            var values = sync.Attributes.ToDictionary(attribute => attribute.Id, attribute => attribute.Value);

            Assert.That(values[CharacterAttribute.Experience], Is.EqualTo(4_000_000_000L));
            Assert.That(values[CharacterAttribute.NextLevelThreshold], Is.EqualTo(4_176_709_541L));
            Assert.That(values[CharacterAttribute.LevelThreshold], Is.EqualTo(3_924_326_032L));
        }

        [Test]
        public void A_pinned_job_change_reads_the_new_job()
        {
            var sync = AttributeSync.Read(new PacketReader(Hex.Bytes(PinnedSwordsmanHex)));

            Assert.That(sync.Attributes.Single(attribute => attribute.Id == CharacterAttribute.Job).Value,
                Is.EqualTo(Swordsman));
        }

        [TestCase(PinnedSpeedRaisedHex)]
        [TestCase(PinnedLevelEightyHex)]
        [TestCase(PinnedSwordsmanHex)]
        public void A_pinned_sync_writes_back_the_bytes_the_server_sent(string hex)
        {
            var packet = new PacketWriter();

            AttributeSync.Read(new PacketReader(Hex.Bytes(hex))).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(hex)));
        }

        [Test]
        public void A_sync_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => AttributeSync.Read(new PacketReader(Hex.Bytes("00000002" + "04" + "0002" + "2C00000258"))),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
