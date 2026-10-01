using System;
using System.Collections.Generic;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class MapEnteredEvent : IServerEvent
    {
        public MapEnteredEvent(string mapName, EnterType enterType, CharacterState character, BagSync bag,
            bool isBagLocked, IReadOnlyList<BagItem?> equipment)
        {
            MapName = mapName ?? throw new ArgumentNullException(nameof(mapName));
            EnterType = enterType;
            Character = character ?? throw new ArgumentNullException(nameof(character));
            Bag = bag ?? throw new ArgumentNullException(nameof(bag));
            IsBagLocked = isBagLocked;
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        }

        public string MapName { get; }

        public EnterType EnterType { get; }

        public CharacterState Character { get; }

        public BagSync Bag { get; }

        public bool IsBagLocked { get; }

        public IReadOnlyList<BagItem?> Equipment { get; }
    }
}
