using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class PreMoveTimeDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new PreMoveTimeChangedEvent((int)PreMoveTime.Read(packet).Milliseconds);
        }
    }
}
