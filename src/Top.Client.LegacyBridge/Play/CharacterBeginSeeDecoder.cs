using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class CharacterBeginSeeDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var appeared = CharacterBeginSee.Read(packet);

            return new CharacterAppearedEvent(
                CharacterStateMapper.ToCharacterState(appeared.Actor, appeared.Attributes),
                (SeeType)appeared.SeeType);
        }
    }
}
