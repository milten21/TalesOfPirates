using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class KitbagSlot
    {
        public static readonly IReadOnlyCollection<short> DefaultBoatItemIds = new short[] { 3988 };

        private const short EndOfList = -1;

        public KitbagSlot(short gridId, ItemGrid content)
        {
            if (gridId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridId), gridId, "A kitbag grid id is zero or above.");
            }

            GridId = gridId;
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public short GridId { get; }

        public ItemGrid Content { get; }

        public static IReadOnlyList<KitbagSlot> ReadList(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var slots = new List<KitbagSlot>();

            while (true)
            {
                var gridId = packet.ReadShort();

                if (gridId < 0)
                {
                    return slots;
                }

                slots.Add(new KitbagSlot(gridId, ItemGrid.ReadKitbagContent(packet, boatItemIds)));
            }
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<KitbagSlot> slots)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            foreach (var slot in slots)
            {
                packet.WriteShort(slot.GridId);
                slot.Content.WriteKitbagContent(packet);
            }

            packet.WriteShort(EndOfList);
        }
    }
}
