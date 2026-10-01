using System;
using System.Collections.Generic;
using Top.Client.Messages.Play.Data;
using Top.Legacy.Protocol.Messages.Characters;

namespace Top.Client.LegacyBridge.Play
{
    public static class ChangedAttributesMapper
    {
        public static ChangedAttributes ToChangedAttributes(IReadOnlyList<CharacterAttribute> attributes)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            var changed = new ChangedAttributes();

            foreach (var attribute in attributes)
            {
                changed = attribute.Id switch
                {
                    CharacterAttribute.Level => changed with { Level = (int)attribute.Value },
                    CharacterAttribute.Health => changed with { Health = (int)attribute.Value },
                    CharacterAttribute.MaxHealth => changed with { MaxHealth = (int)attribute.Value },
                    CharacterAttribute.Mana => changed with { Mana = (int)attribute.Value },
                    CharacterAttribute.MaxMana => changed with { MaxMana = (int)attribute.Value },
                    CharacterAttribute.Job => changed with { Job = (int)attribute.Value },
                    CharacterAttribute.Experience => changed with { Experience = attribute.Value },
                    CharacterAttribute.LevelThreshold => changed with { LevelThreshold = attribute.Value },
                    CharacterAttribute.NextLevelThreshold => changed with { NextLevelThreshold = attribute.Value },
                    CharacterAttribute.Gold => changed with { Gold = attribute.Value },
                    CharacterAttribute.MoveSpeed => changed with
                    {
                        MoveSpeed = MoveSpeedMapper.ToMoveSpeed((int)attribute.Value)
                    },
                    // TODO: report the other attributes of the block when the client shows them.
                    _ => changed,
                };
            }

            return changed;
        }
    }
}
