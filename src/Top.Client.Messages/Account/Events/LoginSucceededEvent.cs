using System;
using System.Collections.Generic;
using System.Linq;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Messages.Account.Events
{
    public class LoginSucceededEvent : IServerEvent
    {
        public LoginSucceededEvent(IEnumerable<CharacterSlot> characters, int slotCap,
            bool hasSecurityCode)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            Characters = characters.ToArray();
            SlotCap = slotCap;
            HasSecurityCode = hasSecurityCode;
        }

        public IReadOnlyList<CharacterSlot> Characters { get; }

        public int SlotCap { get; }

        public bool HasSecurityCode { get; }
    }
}
