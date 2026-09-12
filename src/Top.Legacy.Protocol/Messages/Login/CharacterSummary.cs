using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Login
{
    public class CharacterSummary
    {
        public const int EquipmentSlots = 34;

        public CharacterSummary(string name, string job, ushort level, ushort typeId,
            IReadOnlyList<ushort> equipmentIds)
        {
            if (equipmentIds == null)
            {
                throw new ArgumentNullException(nameof(equipmentIds));
            }

            if (equipmentIds.Count != EquipmentSlots)
            {
                throw new ArgumentException(
                    $"A character carries {EquipmentSlots} equipment ids and {equipmentIds.Count} were given.",
                    nameof(equipmentIds));
            }

            var ids = new ushort[EquipmentSlots];

            for (var slot = 0; slot < ids.Length; slot++)
            {
                ids[slot] = equipmentIds[slot];
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            Job = job ?? throw new ArgumentNullException(nameof(job));
            Level = level;
            TypeId = typeId;
            EquipmentIds = ids;
        }

        public string Name { get; }

        public string Job { get; }

        public ushort Level { get; }

        public ushort TypeId { get; }

        public IReadOnlyList<ushort> EquipmentIds { get; }

        public static IReadOnlyList<CharacterSummary> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var rows = packet.ReadByte();
            var characters = new List<CharacterSummary>(rows);

            for (var row = 0; row < rows; row++)
            {
                if (packet.ReadByte() != 0)
                {
                    characters.Add(Read(packet));
                }
            }

            return characters;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<CharacterSummary> characters)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            if (characters.Count > byte.MaxValue)
            {
                throw new InvalidDataException(
                    $"A list of {characters.Count} characters does not fit a one-byte count.");
            }

            packet.WriteByte((byte)characters.Count);

            foreach (var character in characters)
            {
                packet.WriteByte(1);
                character.Write(packet);
            }
        }

        private static CharacterSummary Read(PacketReader packet)
        {
            var name = packet.ReadString();
            var job = packet.ReadString();
            var level = packet.ReadUShort();
            var typeId = packet.ReadUShort();
            var equipmentIds = new ushort[EquipmentSlots];

            for (var slot = 0; slot < equipmentIds.Length; slot++)
            {
                equipmentIds[slot] = packet.ReadUShort();
            }

            return new CharacterSummary(name, job, level, typeId, equipmentIds);
        }

        private void Write(PacketWriter packet)
        {
            packet.WriteString(Name);
            packet.WriteString(Job);
            packet.WriteUShort(Level);
            packet.WriteUShort(TypeId);

            foreach (var equipmentId in EquipmentIds)
            {
                packet.WriteUShort(equipmentId);
            }
        }
    }
}
