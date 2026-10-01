using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyInviteCancel
    {
        public PartyInviteCancel(byte reason, uint inviterId)
        {
            Reason = reason;
            InviterId = inviterId;
        }

        public byte Reason { get; }

        public uint InviterId { get; }

        public static PartyInviteCancel Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var reason = packet.ReadByte();

            return new PartyInviteCancel(reason, packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(Reason);
            packet.WriteUInt(InviterId);
        }
    }
}
