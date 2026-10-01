using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class SkillSlot
    {
        public const int RangeParameters = 4;

        private const short NoRange = 0;

        public SkillSlot(short id, byte state, byte level, short useSp, short useEndure, short useEnergy,
            int resumeTime, IReadOnlyList<short> range)
        {
            if (range == null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (range.Count != 1 && range.Count != RangeParameters)
            {
                throw new ArgumentException(
                    $"A skill range is one parameter when it has no type and {RangeParameters} otherwise, not {range.Count}.",
                    nameof(range));
            }

            if (range.Count == 1 != (range[0] == NoRange))
            {
                throw new ArgumentException(
                    "A skill range of no type carries one parameter and any other type carries them all.",
                    nameof(range));
            }

            var copy = new short[range.Count];

            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = range[i];
            }

            Id = id;
            State = state;
            Level = level;
            UseSp = useSp;
            UseEndure = useEndure;
            UseEnergy = useEnergy;
            ResumeTime = resumeTime;
            Range = copy;
        }

        public short Id { get; }

        public byte State { get; }

        public byte Level { get; }

        public short UseSp { get; }

        public short UseEndure { get; }

        public short UseEnergy { get; }

        public int ResumeTime { get; }

        public IReadOnlyList<short> Range { get; }

        public static IReadOnlyList<SkillSlot> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var count = packet.ReadShort();

            if (count <= 0)
            {
                return Array.Empty<SkillSlot>();
            }

            var slots = new SkillSlot[count];

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = Read(packet);
            }

            return slots;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<SkillSlot> slots)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (slots.Count > short.MaxValue)
            {
                throw new InvalidDataException($"A skill bag of {slots.Count} slots does not fit a two-byte count.");
            }

            packet.WriteShort((short)slots.Count);

            foreach (var slot in slots)
            {
                slot.Write(packet);
            }
        }

        private static SkillSlot Read(PacketReader packet)
        {
            var id = packet.ReadShort();
            var state = packet.ReadByte();
            var level = packet.ReadByte();
            var useSp = packet.ReadShort();
            var useEndure = packet.ReadShort();
            var useEnergy = packet.ReadShort();
            var resumeTime = packet.ReadInt();
            var rangeType = packet.ReadShort();

            if (rangeType == NoRange)
            {
                return new SkillSlot(id, state, level, useSp, useEndure, useEnergy, resumeTime, new[] { rangeType });
            }

            var range = new short[RangeParameters];
            range[0] = rangeType;

            for (var i = 1; i < range.Length; i++)
            {
                range[i] = packet.ReadShort();
            }

            return new SkillSlot(id, state, level, useSp, useEndure, useEnergy, resumeTime, range);
        }

        private void Write(PacketWriter packet)
        {
            packet.WriteShort(Id);
            packet.WriteByte(State);
            packet.WriteByte(Level);
            packet.WriteShort(UseSp);
            packet.WriteShort(UseEndure);
            packet.WriteShort(UseEnergy);
            packet.WriteInt(ResumeTime);

            foreach (var parameter in Range)
            {
                packet.WriteShort(parameter);
            }
        }
    }
}
