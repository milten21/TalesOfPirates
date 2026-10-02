using System;
using System.Collections.Generic;
using Top.Client.LegacyBridge;

namespace Top.Client.Tools.Debug.Packets;

public class PacketLog : IPacketObserver
{
    private const int Capacity = 1000;

    private readonly ObservedPacket[] _packets = new ObservedPacket[Capacity];

    private readonly object _lock = new object();

    private int _nextSlot;

    private int _filledCount;

    private bool _isRecording;

    public void Start()
    {
        lock (_lock)
        {
            _isRecording = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _isRecording = false;
        }

        Clear();
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_packets);
            _nextSlot = 0;
            _filledCount = 0;
        }
    }

    public void Observe(ObservedPacket packet)
    {
        lock (_lock)
        {
            if (!_isRecording)
            {
                return;
            }

            _packets[_nextSlot] = packet;
            _nextSlot = (_nextSlot + 1) % Capacity;
            _filledCount = Math.Min(_filledCount + 1, Capacity);
        }
    }

    public IReadOnlyList<ObservedPacket> Snapshot()
    {
        lock (_lock)
        {
            var snapshot = new ObservedPacket[_filledCount];
            var oldestSlot = (_nextSlot - _filledCount + Capacity) % Capacity;

            for (var index = 0; index < snapshot.Length; index++)
            {
                snapshot[index] = _packets[(oldestSlot + index) % Capacity];
            }

            return snapshot;
        }
    }
}
