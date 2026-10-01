using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PreMoveTimeTests
    {
        private const string PinnedAfterEnterMapHex = "000001F4";

        [Test]
        public void A_pinned_pre_move_time_carries_the_milliseconds()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedAfterEnterMapHex));

            var preMoveTime = PreMoveTime.Read(packet);

            Assert.That(preMoveTime.Milliseconds, Is.EqualTo(500));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_pre_move_time_writes_back_the_bytes_the_server_sent()
        {
            var packet = new PacketWriter();

            PreMoveTime.Read(new PacketReader(Hex.Bytes(PinnedAfterEnterMapHex))).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedAfterEnterMapHex)));
        }

        [Test]
        public void A_pre_move_time_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => PreMoveTime.Read(new PacketReader(Hex.Bytes("0001F4"))),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
