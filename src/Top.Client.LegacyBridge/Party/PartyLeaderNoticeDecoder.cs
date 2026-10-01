using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class PartyLeaderNoticeDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var notice = PartyLeaderNotice.Read(packet);

            return new CharacterTeamLeaderChangedEvent(notice.WorldId, notice.LeaderId);
        }
    }
}
