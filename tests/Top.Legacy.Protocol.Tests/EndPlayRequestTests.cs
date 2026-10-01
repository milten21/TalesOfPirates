using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class EndPlayRequestTests
    {
        [Test]
        public void A_request_has_no_body()
        {
            var packet = new PacketWriter();
            new EndPlayRequest().Write(packet);

            Assert.That(packet.ToArray(), Is.Empty);
        }

        [Test]
        public void A_request_reads_from_no_bytes()
        {
            Assert.That(EndPlayRequest.Read(new PacketReader(new byte[0])), Is.Not.Null);
        }
    }
}
