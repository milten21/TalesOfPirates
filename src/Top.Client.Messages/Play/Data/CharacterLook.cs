using System;
using System.Collections.Generic;

namespace Top.Client.Messages.Play.Data
{
    public class CharacterLook
    {
        public CharacterLook(int hairId, IReadOnlyList<int> equipmentIds, Fairy? fairy = null)
        {
            HairId = hairId;
            EquipmentIds = equipmentIds ?? throw new ArgumentNullException(nameof(equipmentIds));
            Fairy = fairy;
        }

        public int HairId { get; }

        public IReadOnlyList<int> EquipmentIds { get; }

        public Fairy? Fairy { get; }
    }
}
