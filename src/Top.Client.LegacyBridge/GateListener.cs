using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Top.Client.Messages;
using Top.Client.Messages.Events;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;
using Top.Logging;

namespace Top.Client.LegacyBridge
{
    internal class GateListener : IGateListener
    {
        private readonly int _generation;
        private readonly IReadOnlyDictionary<ushort, Func<PacketReader, IServerEvent?>> _decoders;
        private readonly ConcurrentQueue<QueuedEvent> _queue;
        private readonly object _lock = new object();

        private bool _isClosed;

        public GateListener(
            int generation,
            IReadOnlyDictionary<ushort, Func<PacketReader, IServerEvent?>> decoders,
            ConcurrentQueue<QueuedEvent> queue)
        {
            _generation = generation;
            _decoders = decoders ?? throw new ArgumentNullException(nameof(decoders));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public void OnOpened()
        {
            Enqueue(new ConnectedEvent());
        }

        public void OnReceived(ushort opcode, PacketReader packet)
        {
            if (!_decoders.TryGetValue(opcode, out var decode))
            {
                Log.Info($"packet {opcode} was received and has no decoder");

                return;
            }

            var serverEvent = decode(packet);

            if (serverEvent == null)
            {
                Log.Info($"packet {opcode} was received and carries nothing the client decodes");

                return;
            }

            Enqueue(serverEvent);
        }

        public void OnClosed(CloseReason reason)
        {
            lock (_lock)
            {
                if (_isClosed)
                {
                    return;
                }

                _isClosed = true;
                _queue.Enqueue(new QueuedEvent(_generation, new DisconnectedEvent(ToDisconnectReason(reason))));
            }
        }

        private void Enqueue(IServerEvent serverEvent)
        {
            lock (_lock)
            {
                if (_isClosed)
                {
                    return;
                }

                _queue.Enqueue(new QueuedEvent(_generation, serverEvent));
            }
        }

        private static DisconnectReason ToDisconnectReason(CloseReason reason)
        {
            return reason switch
            {
                CloseReason.CallerClosed => DisconnectReason.ClientClosed,
                CloseReason.Unreachable => DisconnectReason.Unreachable,
                CloseReason.HandshakeFailed => DisconnectReason.HandshakeFailed,
                CloseReason.GateClosed => DisconnectReason.ServerClosed,
                CloseReason.TimedOut => DisconnectReason.TimedOut,
                CloseReason.Broken => DisconnectReason.Broken,
                _ => throw new ArgumentOutOfRangeException(nameof(reason), reason,
                    "The gate closed for a reason the bridge does not know.")
            };
        }
    }
}
