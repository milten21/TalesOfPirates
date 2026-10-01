using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class CharacterEndSeeDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var disappeared = CharacterEndSee.Read(packet);

            return new CharacterDisappearedEvent(disappeared.WorldId, (SeeType)disappeared.SeeType);
        }
    }
}
