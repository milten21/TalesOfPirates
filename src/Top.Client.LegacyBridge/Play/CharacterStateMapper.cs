using System;
using System.Collections.Generic;
using Top.Client.Messages.Play.Data;
using Top.Legacy.Protocol.Messages.Characters;

namespace Top.Client.LegacyBridge.Play
{
    public static class CharacterStateMapper
    {
        public static CharacterState ToCharacterState(ActorCreate actor,
            IReadOnlyList<CharacterAttribute> attributes)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var values = ChangedAttributesMapper.ToChangedAttributes(attributes);

            return new CharacterState(
                actor.WorldId,
                (CharacterControlType)actor.ControlType,
                (int)actor.CharacterId,
                actor.Name,
                MapPointMapper.ToMapPoint(actor.Position),
                actor.Angle,
                actor.ExistState,
                actor.TeamLeaderId,
                actor.GuildId,
                actor.GuildName,
                actor.GuildMotto,
                actor.GuildPermission,
                values.Level ?? 0,
                values.Health ?? 0,
                values.MaxHealth ?? 0,
                values.Mana ?? 0,
                values.MaxMana ?? 0,
                values.Job ?? 0,
                values.Experience ?? 0,
                values.LevelThreshold ?? 0,
                values.NextLevelThreshold ?? 0,
                values.Gold ?? 0,
                values.MoveSpeed ?? 0f,
                ToCharacterLook(actor.Look),
                ToShipLook(actor.Look));
        }

        public static CharacterLook? ToCharacterLook(LookInfo look)
        {
            if (look == null)
            {
                throw new ArgumentNullException(nameof(look));
            }

            if (look.IsBoat)
            {
                return null;
            }

            var equipmentIds = new int[look.Links.Count];

            for (var link = 0; link < equipmentIds.Length; link++)
            {
                equipmentIds[link] = look.Links[link].Id;
            }

            return new CharacterLook(look.HairId, equipmentIds, FairyMapper.ToFairy(look));
        }

        public static ShipLook? ToShipLook(LookInfo look)
        {
            if (look == null)
            {
                throw new ArgumentNullException(nameof(look));
            }

            return look.IsBoat ? new ShipLook(look.Boat.BoatId, look.Boat.Header, look.Boat.Engine) : null;
        }
    }
}
