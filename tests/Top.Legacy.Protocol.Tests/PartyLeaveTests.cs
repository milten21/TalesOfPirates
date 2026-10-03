using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyLeaveTests
    {
        [Test]
        public void A_leave_has_no_body()
        {
            var packet = new PacketWriter();
            new PartyLeave().Write(packet);

            Assert.That(packet.ToArray(), Is.Empty);
        }

        [Test]
        public void A_leave_reads_from_no_bytes()
        {
            Assert.That(PartyLeave.Read(new PacketReader(new byte[0])), Is.Not.Null);
        }
    }
}
