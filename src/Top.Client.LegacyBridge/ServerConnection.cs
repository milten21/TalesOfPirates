using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Top.Client.Messages;
using Top.Client.Messages.Events;
using Top.Legacy.Protocol;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;
using Top.Logging;

namespace Top.Client.LegacyBridge
{
    public partial class ServerConnection : IServerConnection
    {
        private readonly Dictionary<ushort, Func<PacketReader, IServerEvent?>> _decoders =
            new Dictionary<ushort, Func<PacketReader, IServerEvent?>>();

        private readonly Dictionary<Type, Encoder> _encoders = new Dictionary<Type, Encoder>();

        private readonly Dictionary<Type, List<Action<IServerEvent>>> _handlers =
            new Dictionary<Type, List<Action<IServerEvent>>>();

        private readonly ConcurrentQueue<QueuedEvent> _queue = new ConcurrentQueue<QueuedEvent>();

        private readonly Func<string, int, bool, IGateConnection> _gateConnectionFactory;

        private readonly IPacketObserver? _packetObserver;

        private IGateConnection? _gateConnection;
        private int _generation;
        private uint _packetId;
        private bool _isDrainPaused;

        public ServerConnection(IPacketObserver? packetObserver = null) : this(new GateSettings(), packetObserver)
        {
        }

        private ServerConnection(GateSettings settings, IPacketObserver? packetObserver)
            : this(settings, (host, port, isEncrypted) => new GateConnection(host, port, isEncrypted, settings),
                packetObserver)
        {
        }

        public ServerConnection(GateSettings settings, Func<string, int, bool, IGateConnection> gateConnectionFactory,
            IPacketObserver? packetObserver = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _gateConnectionFactory =
                gateConnectionFactory ?? throw new ArgumentNullException(nameof(gateConnectionFactory));
            _packetObserver = packetObserver;

            RegisterMessages(settings);
        }

        public bool IsOpen => _gateConnection != null;

        public bool IsLost { get; private set; }

        public void Connect(string host, int port, bool isEncrypted)
        {
            Close();

            IsLost = false;
            _generation++;
            _packetId = 0;

            var gateConnection = _gateConnectionFactory(host, port, isEncrypted);
            _gateConnection = gateConnection;

            gateConnection.Open(new GateListener(_generation, _decoders, _queue, _packetObserver));
        }

        public void Close()
        {
            var gate = _gateConnection;
            _gateConnection = null;

            gate?.Dispose();
        }

        public void CloseAsLost()
        {
            IsLost = true;

            Close();
        }

        public void PauseDrain()
        {
            _isDrainPaused = true;
        }

        public void ResumeDrain()
        {
            _isDrainPaused = false;
        }

        public void Drain()
        {
            var count = _queue.Count;

            for (var i = 0; i < count && !_isDrainPaused && _queue.TryDequeue(out var queued); i++)
            {
                if (queued.Generation != _generation)
                {
                    continue;
                }

                if (queued.Event is DisconnectedEvent)
                {
                    _gateConnection = null;
                }

                Deliver(queued.Event);
            }
        }

        public void Send(IServerCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!_encoders.TryGetValue(command.GetType(), out var encoder))
            {
                throw new ArgumentException($"{command.GetType().Name} has no encoder.", nameof(command));
            }

            if (_gateConnection == null)
            {
                Log.Warning($"{command.GetType().Name} was sent with no connection to a server and was dropped");

                return;
            }

            var packet = new PacketWriter();
            encoder.Encode(command, packet);
            var bytes = packet.ToArray();

            _packetObserver?.Observe(new ObservedPacket(DateTime.Now, PacketDirection.Outgoing, encoder.Opcode, bytes,
                [command], null));

            _gateConnection.Send(encoder.Opcode, payload => payload.WriteRaw(bytes));
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IServerEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (typeof(TEvent).IsInterface || typeof(TEvent).IsAbstract)
            {
                throw new ArgumentException(
                    $"A subscription names one event type, and {typeof(TEvent).Name} would never fire.");
            }

            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = new List<Action<IServerEvent>>();
                _handlers.Add(typeof(TEvent), handlers);
            }

            Action<IServerEvent> untyped = serverEvent => handler((TEvent)serverEvent);
            handlers.Add(untyped);

            return new Subscription(handlers, untyped);
        }

        private void AddDecoder(ushort opcode, Func<PacketReader, IServerEvent?> decode)
        {
            _decoders.Add(opcode, decode);
        }

        private void AddEncoder<TCommand>(ushort opcode, Action<TCommand, PacketWriter> encode)
            where TCommand : IServerCommand
        {
            _encoders.Add(typeof(TCommand), new Encoder(opcode, (command, packet) =>
                encode((TCommand)command, packet)));
        }

        private uint IssuePacketId()
        {
            return ++_packetId;
        }

        private void Deliver(IServerEvent serverEvent)
        {
            if (!_handlers.TryGetValue(serverEvent.GetType(), out var handlers))
            {
                return;
            }

            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    handler(serverEvent);
                }
                catch (Exception failure)
                {
                    Log.Error($"a handler for {serverEvent.GetType().Name} failed", failure);
                }
            }
        }

        private class Encoder
        {
            private readonly Action<IServerCommand, PacketWriter> _encode;

            public Encoder(ushort opcode, Action<IServerCommand, PacketWriter> encode)
            {
                Opcode = opcode;
                _encode = encode;
            }

            public ushort Opcode { get; }

            public void Encode(IServerCommand command, PacketWriter packet)
            {
                _encode(command, packet);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly List<Action<IServerEvent>> _handlers;
            private Action<IServerEvent>? _handler;

            public Subscription(List<Action<IServerEvent>> handlers, Action<IServerEvent> handler)
            {
                _handlers = handlers;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_handler == null)
                {
                    return;
                }

                _handlers.Remove(_handler);
                _handler = null;
            }
        }
    }
}
