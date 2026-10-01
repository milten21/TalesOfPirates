using System;
using System.Linq;
using Top.Client.Messages.Account.Data;
using Top.Client.Messages.Play.Data;
using Top.Legacy.Protocol.Messages.Account;

namespace Top.Client.LegacyBridge.Account
{
    public static class CharacterSlotMapper
    {
        private const int NoHair = 0;

        public static CharacterSlot ToCharacterSlot(CharacterSummary character)
        {
            if (character == null)
            {
                throw new ArgumentNullException(nameof(character));
            }

            var look = new CharacterLook(NoHair, character.EquipmentIds.Select(id => (int)id).ToArray());

            return new CharacterSlot(character.Name, character.Job, character.Level, character.TypeId, look);
        }
    }
}
