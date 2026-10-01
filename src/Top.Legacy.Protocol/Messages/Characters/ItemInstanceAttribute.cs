using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class ItemInstanceAttribute
    {
        public const int Count = 5;

        public const short Strength = 26;
        public const short Agility = 27;
        public const short Accuracy = 28;
        public const short Constitution = 29;
        public const short Spirit = 30;

        public ItemInstanceAttribute(short id, short value)
        {
            Id = id;
            Value = value;
        }

        public short Id { get; }

        public short Value { get; }

        public static IReadOnlyList<ItemInstanceAttribute> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var attributes = new ItemInstanceAttribute[Count];

            for (var i = 0; i < attributes.Length; i++)
            {
                var id = packet.ReadShort();
                attributes[i] = new ItemInstanceAttribute(id, packet.ReadShort());
            }

            return attributes;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<ItemInstanceAttribute> attributes)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            if (attributes.Count != Count)
            {
                throw new ArgumentException(
                    $"An item carries {Count} instance attributes and {attributes.Count} were given.",
                    nameof(attributes));
            }

            foreach (var attribute in attributes)
            {
                packet.WriteShort(attribute.Id);
                packet.WriteShort(attribute.Value);
            }
        }
    }
}
