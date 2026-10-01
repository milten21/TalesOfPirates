using System;
using Top.Client.Messages;
using Top.Client.Messages.Account.Events;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class DeleteCharacterDecoder
    {
        private const ushort Deleted = 0;

        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = DeleteCharacterReply.Read(packet).Code;

            if (code == Deleted)
            {
                return new CharacterDeletedEvent();
            }

            return new DeleteCharacterFailedEvent(code, ServerError.ToText(code));
        }
    }
}
