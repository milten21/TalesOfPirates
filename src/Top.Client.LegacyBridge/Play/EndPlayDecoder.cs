using System;
using System.Linq;
using Top.Client.LegacyBridge.Account;
using Top.Client.Messages;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class EndPlayDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var reply = EndPlayReply.Read(packet);

            if (reply.IsRefused)
            {
                return new EndPlayFailedEvent(reply.Code, ServerError.ToText(reply.Code));
            }

            return new PlayEndedEvent(reply.Characters.Select(CharacterSlotMapper.ToCharacterSlot),
                reply.SlotCap);
        }
    }
}
