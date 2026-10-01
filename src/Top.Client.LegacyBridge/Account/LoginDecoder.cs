using System;
using System.Linq;
using Top.Client.Messages;
using Top.Client.Messages.Account.Events;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class LoginDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var reply = LoginReply.Read(packet);

            if (reply.IsRefused)
            {
                var text = ServerError.ToText(reply.Code);

                return new LoginFailedEvent(reply.Code, text.Length > 0 ? text : reply.RefusalText);
            }

            return new LoginSucceededEvent(reply.Characters.Select(CharacterSlotMapper.ToCharacterSlot),
                reply.SlotCap, reply.HasSecondPassword);
        }
    }
}
