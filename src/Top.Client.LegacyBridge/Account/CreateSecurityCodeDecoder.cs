using System;
using Top.Client.Messages;
using Top.Client.Messages.Account.Events;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class CreateSecurityCodeDecoder
    {
        private const ushort Created = 0;

        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = CreateSecondPasswordReply.Read(packet).Code;

            if (code == Created)
            {
                return new SecurityCodeCreatedEvent();
            }

            return new CreateSecurityCodeFailedEvent(code, ServerError.ToText(code));
        }
    }
}
