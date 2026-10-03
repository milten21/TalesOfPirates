using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyInviteRequestTests
    {
        private const string InvitedName = "Anne";

        private const string RequestBytes = "0005" + "416E6E65" + "00";

        [Test]
        public void A_request_reads_the_invited_name()
        {
            var request = PartyInviteRequest.Read(new PacketReader(Hex.Bytes(RequestBytes)));

            Assert.That(request.InvitedName, Is.EqualTo(InvitedName));
        }

        [Test]
        public void A_request_is_the_invited_name_as_a_string_and_nothing_else()
        {
            var packet = new PacketWriter();
            new PartyInviteRequest(InvitedName).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(RequestBytes)));
        }

        [Test]
        public void A_request_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyInviteRequest.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
