using System;
using Top.Client.Messages;
using Top.Client.Messages.Account.Events;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class ChangeSecurityCodeDecoder
    {
        private const ushort Changed = 0;

        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = ChangeSecondPasswordReply.Read(packet).Code;

            if (code == Changed)
            {
                return new SecurityCodeChangedEvent();
            }

            return new ChangeSecurityCodeFailedEvent(code, ServerError.ToText(code));
        }
    }
}
