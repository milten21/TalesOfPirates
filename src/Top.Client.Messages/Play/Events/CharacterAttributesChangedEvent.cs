using System;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Play.Events
{
    public class CharacterAttributesChangedEvent : IServerEvent
    {
        public CharacterAttributesChangedEvent(uint worldId, AttributeChangeReason reason, ChangedAttributes attributes)
        {
            WorldId = worldId;
            Reason = reason;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public uint WorldId { get; }

        public AttributeChangeReason Reason { get; }

        public ChangedAttributes Attributes { get; }
    }
}
