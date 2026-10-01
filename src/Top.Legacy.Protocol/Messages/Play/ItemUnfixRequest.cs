using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class ItemUnfixRequest : BeginActionRequest
    {
        public const byte Action = 10;

        public const short FirstFreeGridId = -1;

        public const short GroundGridId = -2;

        public ItemUnfixRequest(uint worldId, uint packetId, byte linkId, short gridId)
            : base(worldId, packetId)
        {
            if (gridId == GroundGridId)
            {
                throw new ArgumentOutOfRangeException(nameof(gridId), gridId,
                    "An unfix to the ground carries a drop position.");
            }

            LinkId = linkId;
            GridId = gridId;
        }

        public ItemUnfixRequest(uint worldId, uint packetId, byte linkId, Point dropPosition)
            : base(worldId, packetId)
        {
            LinkId = linkId;
            GridId = GroundGridId;
            DropPosition = dropPosition;
        }

        public override byte ActionType => Action;

        public byte LinkId { get; }

        public short GridId { get; }

        public Point? DropPosition { get; }

        internal static ItemUnfixRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            var linkId = packet.ReadByte();
            var gridId = packet.ReadShort();

            if (gridId != GroundGridId)
            {
                return new ItemUnfixRequest(worldId, packetId, linkId, gridId);
            }

            var x = packet.ReadInt();

            return new ItemUnfixRequest(worldId, packetId, linkId, new Point(x, packet.ReadInt()));
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WriteByte(LinkId);
            packet.WriteShort(GridId);

            if (DropPosition is { } position)
            {
                packet.WriteInt(position.X);
                packet.WriteInt(position.Y);
            }
        }
    }
}
