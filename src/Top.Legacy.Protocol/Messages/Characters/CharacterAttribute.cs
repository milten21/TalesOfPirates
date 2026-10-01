using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class CharacterAttribute
    {
        public const byte Level = 0;
        public const byte Health = 1;
        public const byte Mana = 2;
        public const byte Job = 4;
        public const byte Gold = 8;
        public const byte Experience = 15;
        public const byte NextLevelThreshold = 16;
        public const byte LevelThreshold = 17;
        public const byte MaxHealth = 31;
        public const byte MaxMana = 32;
        public const byte MoveSpeed = 44;

        private const byte LastExperienceId = 24;

        public CharacterAttribute(byte id, long value)
        {
            Id = id;
            Value = value;
        }

        public byte Id { get; }

        public long Value { get; }

        public static IReadOnlyList<CharacterAttribute> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var count = packet.ReadShort();

            if (count < 0)
            {
                throw new InvalidDataException($"An attribute block counts {count} attributes.");
            }

            var attributes = new CharacterAttribute[count];

            for (var i = 0; i < attributes.Length; i++)
            {
                var id = packet.ReadByte();
                attributes[i] = new CharacterAttribute(id, IsExperience(id) ? packet.ReadUInt() : packet.ReadInt());
            }

            return attributes;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<CharacterAttribute> attributes)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            if (attributes.Count > short.MaxValue)
            {
                throw new InvalidDataException(
                    $"A block of {attributes.Count} attributes does not fit a two-byte count.");
            }

            packet.WriteShort((short)attributes.Count);

            foreach (var attribute in attributes)
            {
                packet.WriteByte(attribute.Id);
                packet.WriteUInt(ToFourBytes(attribute));
            }
        }

        private static bool IsExperience(byte id)
        {
            return id >= Experience && id <= LastExperienceId;
        }

        private static uint ToFourBytes(CharacterAttribute attribute)
        {
            var isExperience = IsExperience(attribute.Id);
            var minimum = isExperience ? uint.MinValue : (long)int.MinValue;
            var maximum = isExperience ? uint.MaxValue : (long)int.MaxValue;

            if (attribute.Value < minimum || attribute.Value > maximum)
            {
                throw new InvalidDataException(
                    $"Attribute {attribute.Id} holds {attribute.Value}, which does not fit its four bytes.");
            }

            return unchecked((uint)attribute.Value);
        }
    }
}
