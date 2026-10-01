using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class AttributeSync
    {
        public AttributeSync(uint worldId, byte attributeType, IReadOnlyList<CharacterAttribute> attributes)
        {
            WorldId = worldId;
            AttributeType = attributeType;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public uint WorldId { get; }

        public byte AttributeType { get; }

        public IReadOnlyList<CharacterAttribute> Attributes { get; }

        public static AttributeSync Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var worldId = packet.ReadUInt();
            var attributeType = packet.ReadByte();

            return new AttributeSync(worldId, attributeType, CharacterAttribute.ReadList(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(WorldId);
            packet.WriteByte(AttributeType);
            CharacterAttribute.WriteList(packet, Attributes);
        }
    }
}
