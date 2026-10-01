using System;
using System.Collections.Generic;
using System.Linq;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class MoveRequest : BeginActionRequest
    {
        public const byte Action = 1;

        public MoveRequest(uint worldId, uint packetId, IReadOnlyList<Point> path)
            : base(worldId, packetId)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public override byte ActionType => Action;

        public IReadOnlyList<Point> Path { get; }

        internal static MoveRequest ReadBody(uint worldId, uint packetId, PacketReader packet)
        {
            return new MoveRequest(worldId, packetId, packet.ReadPoints());
        }

        protected override void WriteBody(PacketWriter packet)
        {
            packet.WritePoints(Path.ToArray());
        }
    }
}
