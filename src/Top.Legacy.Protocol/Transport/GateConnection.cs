using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Top.Legacy.Protocol.Crypto;
using Top.Legacy.Protocol.Packets;
using Top.Logging;

namespace Top.Legacy.Protocol.Transport
{
    public class GateConnection : IGateConnection
    {
        private const int PingFieldCount = 5;

        private readonly string _host;
        private readonly int _port;
        private readonly bool _isEncrypted;
        private readonly GateSettings _settings;

        private readonly BlockingCollection<PendingPacket> _pending = new BlockingCollection<PendingPacket>();

        private readonly object _lock = new object();

        private IGateListener _listener;
        private TcpClient _socket;
        private FrameReader _frameReader;
        private FrameWriter _frameWriter;
        private PacketCipher _cipher;
        private uint _sentCount;
        private bool _isOpened;
        private volatile bool _isClosed;

        public GateConnection(string host, int port, bool isEncrypted, GateSettings settings)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _isEncrypted = isEncrypted;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Open(IGateListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            lock (_lock)
            {
                if (_isOpened || _isClosed)
                {
                    throw new InvalidOperationException("A connection to the gate opens once.");
                }

                _isOpened = true;
                _listener = listener;
            }

            new Thread(Run) { IsBackground = true, Name = "gate receive" }.Start();
        }

        public void Send(ushort opcode, Action<PacketWriter> payload)
        {
            if (_isClosed)
            {
                return;
            }

            var packet = new PacketWriter();
            payload?.Invoke(packet);

            try
            {
                _pending.Add(new PendingPacket(opcode, packet.ToArray()));
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void Dispose()
        {
            Close(CloseReason.CallerClosed);
        }

        private void Run()
        {
            if (!OpenSocket() || !ExchangeKeys())
            {
                return;
            }

            new Thread(WritePackets) { IsBackground = true, Name = "gate send" }.Start();

            ReportOpen();
            ReadPackets();
        }

        private bool OpenSocket()
        {
            var socket = new TcpClient { NoDelay = true };

            try
            {
                socket.Connect(_host, _port);
            }
            catch (Exception failure)
            {
                socket.Dispose();
                Log.Warning($"the gate at {_host}:{_port} did not answer", failure);
                Close(CloseReason.Unreachable);

                return false;
            }

            lock (_lock)
            {
                if (_isClosed)
                {
                    socket.Dispose();

                    return false;
                }

                _socket = socket;

                var stream = socket.GetStream();
                stream.ReadTimeout = (int)_settings.ReadTimeout.TotalMilliseconds;
                _frameReader = new FrameReader(stream, _settings.FrameFormat);
                _frameWriter = new FrameWriter(stream, _settings.FrameFormat);
            }

            return true;
        }

        private bool ExchangeKeys()
        {
            if (!_isEncrypted)
            {
                return true;
            }

            try
            {
                var packet = new PacketReader(_frameReader.ReadPacket());
                var opcode = packet.ReadUShort();

                if (opcode != Opcode.ServerPublicKey)
                {
                    throw new InvalidDataException($"The gate opened with packet {opcode}, not its public key.");
                }

                var key = PacketCipher.GenerateKey();
                var wrapped = ServerPublicKey.Read(packet).WrapKey(key);
                var payload = new PacketWriter();
                payload.WriteString(Convert.ToBase64String(wrapped));

                WriteToGate(Opcode.PrivateKey, payload.ToArray());
                _cipher = new PacketCipher(key);

                return true;
            }
            catch (Exception failure)
            {
                var reason = ReasonFor(failure);

                if (!_isClosed)
                {
                    Log.Warning("the key exchange with the gate failed", failure);
                }

                Close(reason == CloseReason.Broken ? CloseReason.HandshakeFailed : reason);

                return false;
            }
        }

        private void ReadPackets()
        {
            try
            {
                while (true)
                {
                    var frame = _frameReader.ReadPacket();
                    var packet = new PacketReader(_cipher == null ? frame : _cipher.Decrypt(frame));
                    var opcode = packet.ReadUShort();

                    switch (opcode)
                    {
                        case Opcode.Ping:
                            EchoPing(packet);
                            break;

                        case Opcode.CheckPing:
                            Send(Opcode.CheckPingReply, null);
                            break;

                        case Opcode.GroupPing:
                            Send(Opcode.GroupPingReply, null);
                            break;

                        default:
                            Deliver(opcode, packet);
                            break;
                    }
                }
            }
            catch (Exception failure)
            {
                var reason = ReasonFor(failure);

                if (!_isClosed && reason == CloseReason.Broken)
                {
                    Log.Warning("the connection to the gate broke", failure);
                }

                Close(reason);
            }
        }

        private void WritePackets()
        {
            var idle = (int)_settings.IdleInterval.TotalMilliseconds;

            try
            {
                while (!_pending.IsAddingCompleted)
                {
                    if (_pending.TryTake(out var pending, idle))
                    {
                        WriteToGate(pending.Opcode, pending.Payload);
                    }
                    else if (!_pending.IsAddingCompleted)
                    {
                        _frameWriter.WriteIdleFrame();
                    }
                }
            }
            catch (Exception failure)
            {
                var reason = ReasonFor(failure);

                if (!_isClosed && reason == CloseReason.Broken)
                {
                    Log.Warning("the gate stopped taking packets", failure);
                }

                Close(reason);
            }
        }

        private void ReportOpen()
        {
            if (_isClosed)
            {
                return;
            }

            try
            {
                _listener.OnOpened();
            }
            catch (Exception failure)
            {
                Log.Error("the handler for an open connection failed", failure);
            }
        }

        private void Deliver(ushort opcode, PacketReader packet)
        {
            try
            {
                _listener.OnReceived(opcode, packet);
            }
            catch (Exception failure)
            {
                Log.Error($"the handler for packet {opcode} failed", failure);
            }
        }

        private void EchoPing(PacketReader packet)
        {
            var fields = new uint[PingFieldCount];

            for (var i = 0; i < fields.Length && packet.Remaining >= sizeof(uint); i++)
            {
                fields[i] = packet.ReadUInt();
            }

            Send(Opcode.PingReply, writer =>
            {
                foreach (var field in fields)
                {
                    writer.WriteUInt(field);
                }
            });
        }

        private void WriteToGate(ushort opcode, byte[] payload)
        {
            var writer = new PacketWriter(opcode);
            writer.WriteUInt(_sentCount);
            writer.WriteRaw(payload);

            var packet = writer.ToArray();

            if (_cipher != null)
            {
                packet = _cipher.Encrypt(packet);
            }

            if (packet.Length > _settings.FrameFormat.MaxSendPacketLength)
            {
                Log.Warning($"packet {opcode} of {packet.Length} bytes passes what the gate reads and was dropped");

                return;
            }

            _frameWriter.WritePacket(packet);
            _sentCount++;
        }

        private void Close(CloseReason reason)
        {
            TcpClient socket;
            IGateListener listener;

            lock (_lock)
            {
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;
                socket = _socket;
                listener = _listener;
            }

            _pending.CompleteAdding();
            socket?.Dispose();

            if (listener == null)
            {
                return;
            }

            try
            {
                listener.OnClosed(reason);
            }
            catch (Exception failure)
            {
                Log.Error($"the handler for the close reason {reason} failed", failure);
            }
        }

        private static CloseReason ReasonFor(Exception failure)
        {
            switch (failure)
            {
                case EndOfStreamException _:
                    return CloseReason.GateClosed;
                case IOException broken when broken.InnerException is SocketException socket
                                             && socket.SocketErrorCode == SocketError.TimedOut:
                    return CloseReason.TimedOut;
                default:
                    return CloseReason.Broken;
            }
        }

        private class PendingPacket
        {
            public PendingPacket(ushort opcode, byte[] payload)
            {
                Opcode = opcode;
                Payload = payload;
            }

            public ushort Opcode { get; }

            public byte[] Payload { get; }
        }
    }
}
