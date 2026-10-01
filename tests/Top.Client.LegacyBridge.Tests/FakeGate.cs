using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;

namespace Top.Client.LegacyBridge.Tests
{
    public class FakeGate : IGateConnection
    {
        public FakeGate(string host, int port, bool isEncrypted)
        {
            Host = host;
            Port = port;
            IsEncrypted = isEncrypted;
        }

        public string Host { get; }

        public int Port { get; }

        public bool IsEncrypted { get; }

        public IGateListener Listener { get; private set; }

        public List<(ushort Opcode, byte[] Payload)> Sent { get; } = [];

        public bool IsDisposed { get; private set; }

        public bool ReportsCloseOnDispose { get; set; } = true;

        public void Open(IGateListener listener)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
        }

        public void Send(ushort opcode, Action<PacketWriter> payload)
        {
            var packet = new PacketWriter();
            payload?.Invoke(packet);

            Sent.Add((opcode, packet.ToArray()));
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            if (ReportsCloseOnDispose)
            {
                Listener?.OnClosed(CloseReason.CallerClosed);
            }
        }
    }
}
