using System;
using Top.Client.Messages.Chat.Commands;
using Top.Legacy.Protocol.Messages.Chat;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Chat
{
    public static class SayEncoder
    {
        public static void Encode(SayCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new SayRequest(command.Text).Write(packet);
        }
    }
}
