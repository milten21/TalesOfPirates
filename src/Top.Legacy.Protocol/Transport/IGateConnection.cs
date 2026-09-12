using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Transport
{
    public interface IGateConnection : IDisposable
    {
        void Open(IGateListener listener);

        void Send(ushort opcode, Action<PacketWriter> payload);
    }
}
