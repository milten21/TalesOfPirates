using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class PartyInviteCancelTests
    {
        private const byte Timeout = 2;
        private const uint InviterId = 9;

        private const string CancelBytes = "02" + "00000009";

        [Test]
        public void A_cancel_reads_the_reason_and_then_the_inviter_id()
        {
            var cancel = PartyInviteCancel.Read(new PacketReader(Hex.Bytes(CancelBytes)));

            Assert.That(cancel.Reason, Is.EqualTo(Timeout));
            Assert.That(cancel.InviterId, Is.EqualTo(InviterId));
        }

        [Test]
        public void A_cancel_writes_the_reason_byte_then_the_id()
        {
            var packet = new PacketWriter();
            new PartyInviteCancel(Timeout, InviterId).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(CancelBytes)));
        }

        [Test]
        public void A_cancel_round_trips()
        {
            var packet = new PacketWriter();
            new PartyInviteCancel(4, 42).Write(packet);

            var read = PartyInviteCancel.Read(new PacketReader(packet.ToArray()));

            Assert.That(read.Reason, Is.EqualTo(4));
            Assert.That(read.InviterId, Is.EqualTo(42u));
        }

        [Test]
        public void A_cancel_with_no_bytes_throws()
        {
            Assert.That(
                () => PartyInviteCancel.Read(new PacketReader(Array.Empty<byte>())),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
