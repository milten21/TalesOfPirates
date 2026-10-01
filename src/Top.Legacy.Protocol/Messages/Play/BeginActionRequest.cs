using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public abstract class BeginActionRequest
    {
        private static readonly Dictionary<byte, Func<uint, uint, PacketReader, BeginActionRequest>> BodyReaders =
            new Dictionary<byte, Func<uint, uint, PacketReader, BeginActionRequest>>
            {
                [MoveRequest.Action] = MoveRequest.ReadBody,
                [ItemThrowRequest.Action] = ItemThrowRequest.ReadBody,
                [ItemUnfixRequest.Action] = ItemUnfixRequest.ReadBody,
                [ItemUseRequest.Action] = ItemUseRequest.ReadBody,
                [ItemMoveRequest.Action] = ItemMoveRequest.ReadBody,
                [ItemDeleteRequest.Action] = ItemDeleteRequest.ReadBody,
                [TemporaryKitbagDragRequest.Action] = TemporaryKitbagDragRequest.ReadBody,
            };

        protected BeginActionRequest(uint worldId, uint packetId)
        {
            WorldId = worldId;
            PacketId = packetId;
        }

        public uint WorldId { get; }

        public uint PacketId { get; }

        public abstract byte ActionType { get; }

        public static BeginActionRequest Read(PacketReader packet)
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
                : new UnreadActionRequest(worldId, packetId, actionType);
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
