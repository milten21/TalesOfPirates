using System;
using Top.Client.LegacyBridge.Play;
using Top.Client.Messages;
using Top.Client.Messages.Party.Events;
using Top.Legacy.Protocol.Messages.Party;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Party
{
    public static class PartyMemberVitalsDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var vitals = PartyMemberVitals.Read(packet);

            return new PartyMemberVitalsChangedEvent(vitals.MemberId, new Messages.Party.Data.PartyMemberVitals(
                vitals.Level,
                vitals.Health,
                vitals.MaxHealth,
                vitals.Mana,
                vitals.MaxMana,
                vitals.Look.TypeId,
                CharacterStateMapper.ToCharacterLook(vitals.Look)));
        }
    }
}
