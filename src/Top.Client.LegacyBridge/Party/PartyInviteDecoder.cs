using System;
using Top.Client.Messages;
using Top.Client.Messages.Party.Events;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class PartyInviteDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var invite = PartyInvite.Read(packet);

            return new PartyInvitedEvent(invite.InviterId, invite.InviterName);
        }
    }
}
