using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Messages.Play.Events
{
    public class PlayEndedEvent : IServerEvent
    {
        public PlayEndedEvent(IEnumerable<CharacterSlot> characters, int slotCap)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            Characters = characters.ToArray();
            SlotCap = slotCap;
        }

        public IReadOnlyList<CharacterSlot> Characters { get; }

        public int SlotCap { get; }
    }
}
