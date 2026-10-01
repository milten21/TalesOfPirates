using System;
using Top.Client.Messages;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class AttributeSyncDecoder
    {
        private static readonly ChangedAttributes Unchanged = new ChangedAttributes();

        public static IServerEvent? Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var sync = AttributeSync.Read(packet);

            var changed = ChangedAttributesMapper.ToChangedAttributes(sync.Attributes);

            return changed == Unchanged
                ? null
                : new CharacterAttributesChangedEvent(sync.WorldId, (AttributeChangeReason)sync.AttributeType,
                    changed);
        }
    }
}
