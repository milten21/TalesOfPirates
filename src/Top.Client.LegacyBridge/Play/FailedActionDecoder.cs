using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class FailedActionDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var notice = FailedActionNotice.Read(packet);

            return new ActionFailedEvent(notice.WorldId, notice.ActionType, (ActionFailureReason)notice.Reason);
        }
    }
}
