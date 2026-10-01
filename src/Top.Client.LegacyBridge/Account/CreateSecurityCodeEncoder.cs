using System;
using Top.Client.Messages.Account.Commands;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class CreateSecurityCodeEncoder
    {
        public static void Encode(CreateSecurityCodeCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new CreateSecondPasswordRequest(command.Code).Write(packet);
        }
    }
}
