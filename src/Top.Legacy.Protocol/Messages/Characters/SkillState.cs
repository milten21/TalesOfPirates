using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class SkillState
    {
        public SkillState(byte id, byte level, int duration, int startTime)
        {
            Id = id;
            Level = level;
            Duration = duration;
            StartTime = startTime;
        }

        public byte Id { get; }

        public byte Level { get; }

        public int Duration { get; }

        public int StartTime { get; }

        public static IReadOnlyList<SkillState> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var count = packet.ReadByte();
            var states = new SkillState[count];

            for (var i = 0; i < states.Length; i++)
            {
                var id = packet.ReadByte();
                var level = packet.ReadByte();
                var duration = packet.ReadInt();
                states[i] = new SkillState(id, level, duration, packet.ReadInt());
            }

            return states;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<SkillState> states)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (states == null)
            {
                throw new ArgumentNullException(nameof(states));
            }

            if (states.Count > byte.MaxValue)
            {
                throw new InvalidDataException($"A list of {states.Count} skill states does not fit a one-byte count.");
            }

            packet.WriteByte((byte)states.Count);

            foreach (var state in states)
            {
                packet.WriteByte(state.Id);
                packet.WriteByte(state.Level);
                packet.WriteInt(state.Duration);
                packet.WriteInt(state.StartTime);
            }
        }
    }
}
