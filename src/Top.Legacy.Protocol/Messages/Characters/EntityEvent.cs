using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class EntityEvent
    {
        public EntityEvent(int entityId, byte entityType, ushort eventId, string eventName)
        {
            EntityId = entityId;
            EntityType = entityType;
            EventId = eventId;
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        }

        public int EntityId { get; }

        public byte EntityType { get; }

        public ushort EventId { get; }

        public string EventName { get; }

        public static EntityEvent Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var entityId = packet.ReadInt();
            var entityType = packet.ReadByte();
            var eventId = packet.ReadUShort();

            return new EntityEvent(entityId, entityType, eventId, packet.ReadString());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteInt(EntityId);
            packet.WriteByte(EntityType);
            packet.WriteUShort(EventId);
            packet.WriteString(EventName);
        }
    }
}
