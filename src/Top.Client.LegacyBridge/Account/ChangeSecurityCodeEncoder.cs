using System;
using Top.Client.Messages.Account.Commands;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class ChangeSecurityCodeEncoder
    {
        public static void Encode(ChangeSecurityCodeCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new ChangeSecondPasswordRequest(command.CurrentCode, command.NewCode).Write(packet);
        }
    }
}
