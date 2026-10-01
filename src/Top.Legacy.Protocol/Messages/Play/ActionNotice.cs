using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public abstract class ActionNotice
    {
        private static readonly Dictionary<byte, Func<uint, uint, PacketReader, ActionNotice>> BodyReaders =
            new Dictionary<byte, Func<uint, uint, PacketReader, ActionNotice>>
            {
                [MoveNotice.Action] = MoveNotice.ReadBody,
                [LookNotice.Action] = LookNotice.ReadBody,
                [KitbagNotice.Action] = KitbagNotice.ReadBody,
                [ItemFailedNotice.Action] = ItemFailedNotice.ReadBody,
                [TemporaryKitbagNotice.Action] = TemporaryKitbagNotice.ReadBody,
            };

        protected ActionNotice(uint worldId, uint packetId)
        {
            WorldId = worldId;
            PacketId = packetId;
        }

        public uint WorldId { get; }

        public uint PacketId { get; }

        public abstract byte ActionType { get; }

        public static ActionNotice Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var worldId = packet.ReadUInt();
            var packetId = packet.ReadUInt();
            var actionType = packet.ReadByte();

            return BodyReaders.TryGetValue(actionType, out var readBody)
                ? readBody(worldId, packetId, packet)
                : new UnreadActionNotice(worldId, packetId, actionType);
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(WorldId);
            packet.WriteUInt(PacketId);
            packet.WriteByte(ActionType);
            WriteBody(packet);
        }

        protected abstract void WriteBody(PacketWriter packet);
    }
}
