using System;
using Top.Client.Messages.Account.Commands;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Account
{
    public static class DeleteCharacterEncoder
    {
        public static void Encode(DeleteCharacterCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new DeleteCharacterRequest((byte)command.CharacterIndex, command.Code).Write(packet);
        }
    }
}
