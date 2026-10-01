using System;
using Top.Client.Messages.Play.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class BeginPlayEncoder
    {
        public static void Encode(BeginPlayCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new BeginPlayRequest((byte)command.CharacterIndex).Write(packet);
        }
    }
}
