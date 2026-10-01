using System;
using Top.Client.Messages;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class PartyInviteCancelDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var cancel = PartyInviteCancel.Read(packet);

            return new PartyInviteEndedEvent(cancel.InviterId, (PartyInviteEndReason)cancel.Reason);
        }
    }
}
