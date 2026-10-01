using System;
using Top.Client.Messages.Play.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class EndPlayEncoder
    {
        public static void Encode(EndPlayCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new EndPlayRequest().Write(packet);
        }
    }
}
