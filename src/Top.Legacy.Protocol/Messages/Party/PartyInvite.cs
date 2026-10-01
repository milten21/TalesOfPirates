using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyInvite
    {
        public PartyInvite(string inviterName, uint inviterId, ushort icon)
        {
            InviterName = inviterName ?? throw new ArgumentNullException(nameof(inviterName));
            InviterId = inviterId;
            Icon = icon;
        }

        public string InviterName { get; }

        public uint InviterId { get; }

        public ushort Icon { get; }

        public static PartyInvite Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var inviterName = packet.ReadString();
            var inviterId = packet.ReadUInt();

            return new PartyInvite(inviterName, inviterId, packet.ReadUShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteString(InviterName);
            packet.WriteUInt(InviterId);
            packet.WriteUShort(Icon);
        }
    }
}
