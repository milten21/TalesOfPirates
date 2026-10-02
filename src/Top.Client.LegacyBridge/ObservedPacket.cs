using System;
using System.Collections.Generic;

namespace Top.Client.LegacyBridge
{
    public class ObservedPacket
    {
        public ObservedPacket(DateTime time, PacketDirection direction, ushort opcode, ReadOnlyMemory<byte> bytes,
            IReadOnlyList<object> messages, EmptyPacketReason? emptyReason)
        {
            Time = time;
            Direction = direction;
            Opcode = opcode;
            Bytes = bytes;
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
            EmptyReason = emptyReason;
        }

        public DateTime Time { get; }

        public PacketDirection Direction { get; }

        public ushort Opcode { get; }

        public ReadOnlyMemory<byte> Bytes { get; }

        public IReadOnlyList<object> Messages { get; }

        public EmptyPacketReason? EmptyReason { get; }
    }
}
