using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class ShortcutSlot
    {
        public const int Count = 36;

        public ShortcutSlot(byte type, short gridId)
        {
            Type = type;
            GridId = gridId;
        }

        public byte Type { get; }

        public short GridId { get; }

        public static IReadOnlyList<ShortcutSlot> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var slots = new ShortcutSlot[Count];

            for (var i = 0; i < slots.Length; i++)
            {
                var type = packet.ReadByte();
                slots[i] = new ShortcutSlot(type, packet.ReadShort());
            }

            return slots;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<ShortcutSlot> slots)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (slots.Count != Count)
            {
                throw new ArgumentException(
                    $"A shortcut bar carries {Count} slots and {slots.Count} were given.", nameof(slots));
            }

            foreach (var slot in slots)
            {
                packet.WriteByte(slot.Type);
                packet.WriteShort(slot.GridId);
            }
        }
    }
}
