using System;
using Top.Client.Messages;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class BeginPlayDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return BeginPlayFailedEventMapper.ToBeginPlayFailedEvent(BeginPlayReply.Read(packet).Code);
        }
    }
}
