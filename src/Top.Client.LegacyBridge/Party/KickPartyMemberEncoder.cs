using System;
using Top.Client.Messages.Party.Commands;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class KickPartyMemberEncoder
    {
        public static void Encode(KickPartyMemberCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new PartyKick(command.MemberId).Write(packet);
        }
    }
}
