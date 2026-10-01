using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class EndActionRequestTests
    {
        private const string PinnedEndActionHex = "";

        [Test]
        public void A_request_writes_the_empty_body_on_which_the_reference_server_cancelled_a_move()
        {
            var packet = new PacketWriter();

            new EndActionRequest().Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedEndActionHex)));
        }

        [Test]
        public void A_request_reads_from_no_bytes()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedEndActionHex));

            Assert.That(EndActionRequest.Read(packet), Is.Not.Null);
            Assert.That(packet.Remaining, Is.Zero);
        }
    }
}
