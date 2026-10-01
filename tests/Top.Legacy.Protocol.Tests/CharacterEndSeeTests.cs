using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class CharacterEndSeeTests
    {
        private const string PinnedDisappearHex = "00" + "B00028CA";

        [Test]
        public void A_pinned_disappear_names_the_world_id_and_the_see_type()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedDisappearHex));

            var disappeared = CharacterEndSee.Read(packet);

            Assert.That(disappeared.SeeType, Is.Zero, "a new character");
            Assert.That(disappeared.WorldId, Is.EqualTo(0xB00028CA));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_disappear_writes_back_the_bytes_the_server_sent()
        {
            var packet = new PacketWriter();

            CharacterEndSee.Read(new PacketReader(Hex.Bytes(PinnedDisappearHex))).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedDisappearHex)));
        }

        [Test]
        public void A_disappear_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => CharacterEndSee.Read(new PacketReader(Hex.Bytes("00B00028"))),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
