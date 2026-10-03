using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyInviteRequest
    {
        public PartyInviteRequest(string invitedName)
        {
            InvitedName = invitedName ?? throw new ArgumentNullException(nameof(invitedName));
        }

        public string InvitedName { get; }

        public static PartyInviteRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new PartyInviteRequest(packet.ReadString());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteString(InvitedName);
        }
    }
}
