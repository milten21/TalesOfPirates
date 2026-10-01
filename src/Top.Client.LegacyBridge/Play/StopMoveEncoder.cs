using System;
using Top.Client.Messages.Play.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class StopMoveEncoder
    {
        public static void Encode(StopMoveCommand command, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            new EndActionRequest().Write(packet);
        }
    }
}
