using System;
using System.Collections.Generic;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class CharacterLookChangedEvent : IServerEvent
    {
        public CharacterLookChangedEvent(uint worldId, CharacterLook look, IReadOnlyList<BagItem?> equipment)
        {
            WorldId = worldId;
            Look = look ?? throw new ArgumentNullException(nameof(look));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public uint WorldId { get; }

        public CharacterLook Look { get; }

        public IReadOnlyList<BagItem?> Equipment { get; }
    }
}
