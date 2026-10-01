using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages;
using Top.Client.Messages.Party.Data;
using Top.Client.Messages.Party.Events;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class PartyRefreshDecoder
    {
        private const int MinimumMembers = 2;

        // TODO: a chat message for the join, the leave and the kick, once chat exists.
        public static IServerEvent? Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var refresh = PartyRefresh.Read(packet);

            if (ToChangeKind(refresh.Kind) is not { } kind || refresh.Members.Count == 0)
            {
                return null;
            }

            var members = refresh.Members.Select(member => new PartyMember(member.CharacterId, member.Name)).ToList();
            var changedMember = members[members.Count - 1];

            if (kind != PartyChangeKind.Joined)
            {
                members.RemoveAt(members.Count - 1);
            }

            return new PartyChangedEvent(kind, changedMember.Name,
                members.Count < MinimumMembers ? Array.Empty<PartyMember>() : members);
        }

        private static PartyChangeKind? ToChangeKind(byte kind)
        {
            return kind switch
            {
                PartyRefresh.Join => PartyChangeKind.Joined,
                PartyRefresh.Leave => PartyChangeKind.Left,
                PartyRefresh.Kick => PartyChangeKind.Kicked,
                _ => null
            };
        }
    }
}
