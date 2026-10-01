using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class TemporaryKitbagSyncRequestTests
    {
        [Test]
        public void A_request_has_no_body()
        {
            var packet = new PacketWriter();
            new TemporaryKitbagSyncRequest().Write(packet);

            Assert.That(packet.ToArray(), Is.Empty);
        }

        [Test]
        public void A_request_reads_from_no_bytes()
        {
            Assert.That(TemporaryKitbagSyncRequest.Read(new PacketReader(new byte[0])), Is.Not.Null);
        }
    }
}
