using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class FailedActionNoticeTests
    {
        private const string PinnedSecondMoveRefusedHex = "00000001" + "01" + "01";

        [Test]
        public void A_pinned_refusal_names_the_character_the_action_and_the_reason()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedSecondMoveRefusedHex));

            var notice = FailedActionNotice.Read(packet);

            Assert.That(notice.WorldId, Is.EqualTo(1));
            Assert.That(notice.ActionType, Is.EqualTo(1), "a move");
            Assert.That(notice.Reason, Is.EqualTo(1), "the move before it was still going on");
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_refusal_writes_back_the_bytes_the_server_sent()
        {
            var packet = new PacketWriter();

            FailedActionNotice.Read(new PacketReader(Hex.Bytes(PinnedSecondMoveRefusedHex))).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedSecondMoveRefusedHex)));
        }

        [Test]
        public void A_refusal_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => FailedActionNotice.Read(new PacketReader(Hex.Bytes("0000000101"))),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
