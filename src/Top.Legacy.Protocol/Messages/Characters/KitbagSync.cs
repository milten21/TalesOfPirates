using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class KitbagSync
    {
        public const byte InitType = 0;

        public KitbagSync(byte type, short capacity, IReadOnlyList<KitbagSlot> slots)
        {
            if (type != InitType && capacity != 0)
            {
                throw new ArgumentException("Only an init sync carries the kitbag capacity.", nameof(capacity));
            }

            Type = type;
            Capacity = capacity;
            Slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }

        public byte Type { get; }

        public short Capacity { get; }

        public IReadOnlyList<KitbagSlot> Slots { get; }

        public bool IsInit => Type == InitType;

        public static KitbagSync Read(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var type = packet.ReadByte();
            var capacity = type == InitType ? packet.ReadShort() : (short)0;

            return new KitbagSync(type, capacity, KitbagSlot.ReadList(packet, boatItemIds));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(Type);

            if (IsInit)
            {
                packet.WriteShort(Capacity);
            }

            KitbagSlot.WriteList(packet, Slots);
        }
    }
}
