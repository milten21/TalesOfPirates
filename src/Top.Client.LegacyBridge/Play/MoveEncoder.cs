using System;
using System.Linq;
using Top.Client.Messages.Play.Commands;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class MoveEncoder
    {
        public static void Encode(MoveCommand command, uint packetId, PacketWriter packet)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var path = command.Path.Select(MapPointMapper.ToPacketPoint).ToArray();

            new MoveRequest(command.WorldId, packetId, path).Write(packet);
        }
    }
}
