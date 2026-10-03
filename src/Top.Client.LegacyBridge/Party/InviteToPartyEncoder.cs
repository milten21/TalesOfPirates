using System;
using Top.Client.Messages.Party.Commands;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class InviteToPartyEncoder
    {
        public static void Encode(InviteToPartyCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new PartyInviteRequest(command.CharacterName).Write(packet);
        }
    }
}
