using System;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Characters;

namespace Top.Client.LegacyBridge.Play
{
    public static class FairyMapper
    {
        private const int FairyLink = 16;

        private const int FairyAppearanceLink = 24;

        private const int EndurePerStamina = 50;

        public static Fairy? ToFairy(LookInfo look)
        {
            if (look == null)
            {
                throw new ArgumentNullException(nameof(look));
            }

            var fairy = look.Links[FairyLink];

            if (!IsValidItem(fairy))
            {
                return null;
            }

            var appearance = look.Links[FairyAppearanceLink];

            return new Fairy(
                ItemId: fairy.Id,
                AppearanceItemId: appearance.IsEmpty ? null : appearance.Id,
                Level: ToFairyLevel(fairy),
                Stamina: ToStamina(fairy.Endure[0]),
                MaxStamina: ToStamina(fairy.Endure[1]),
                Growth: fairy.Energy[0],
                MaxGrowth: fairy.Energy[1]);
        }

        public static FairyConditionChangedEvent? ToFairyConditionChangedEvent(uint worldId, LookInfo look)
        {
            if (look == null)
            {
                throw new ArgumentNullException(nameof(look));
            }

            var fairy = look.Links[FairyLink];

            return IsValidItem(fairy)
                ? new FairyConditionChangedEvent(worldId, fairy.Id, ToStamina(fairy.Endure[0]), fairy.Energy[0])
                : null;
        }

        private static bool IsValidItem(ItemGrid link)
        {
            return !link.IsEmpty && link.IsValid;
        }

        private static int ToStamina(short endure)
        {
            return endure / EndurePerStamina;
        }

        private static int ToFairyLevel(ItemGrid fairy)
        {
            return fairy.GetInstanceAttribute(ItemInstanceAttribute.Strength)
                   + fairy.GetInstanceAttribute(ItemInstanceAttribute.Agility)
                   + fairy.GetInstanceAttribute(ItemInstanceAttribute.Accuracy)
                   + fairy.GetInstanceAttribute(ItemInstanceAttribute.Constitution)
                   + fairy.GetInstanceAttribute(ItemInstanceAttribute.Spirit);
        }
    }
}
