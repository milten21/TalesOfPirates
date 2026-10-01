using System;
using System.Collections.Generic;
using System.Linq;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class MoveInfo
    {
        public MoveInfo(short state, short? stopState, IReadOnlyList<Point> points)
        {
            if ((state != 0) != stopState.HasValue)
            {
                throw new ArgumentException(
                    "A move that has ended carries a stop state and a move that goes on carries none.",
                    nameof(stopState));
            }

            State = state;
            StopState = stopState;
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }

        public short State { get; }

        public short? StopState { get; }

        public IReadOnlyList<Point> Points { get; }

        public static MoveInfo Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var state = packet.ReadShort();
            short? stopState = null;

            if (state != 0)
            {
                stopState = packet.ReadShort();
            }

            return new MoveInfo(state, stopState, packet.ReadPoints());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteShort(State);

            if (StopState.HasValue)
            {
                packet.WriteShort(StopState.Value);
            }

            packet.WritePoints(Points.ToArray());
        }
    }
}
