using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.LegacyBridge;
using Top.Client.Tools.Debug.Packets;

namespace Top.Client.Tests.Tools.Debug.Packets;

public class PacketLogTests
{
    private PacketLog _log;

    [SetUp]
    public void CreateLog()
    {
        _log = new PacketLog();
    }

    [Test]
    public void A_started_log_keeps_the_packets_in_the_order_they_arrived()
    {
        _log.Start();

        _log.Observe(BuildPacket(1));
        _log.Observe(BuildPacket(2));
        _log.Observe(BuildPacket(3));

        Assert.That(_log.Snapshot().Select(packet => packet.Opcode), Is.EqualTo(new ushort[] { 1, 2, 3 }));
    }

    [Test]
    public void A_log_that_was_not_started_keeps_nothing()
    {
        _log.Observe(BuildPacket(1));

        Assert.That(_log.Snapshot(), Is.Empty);
    }

    [Test]
    public void A_stopped_log_forgets_its_packets_and_keeps_nothing_more()
    {
        _log.Start();
        _log.Observe(BuildPacket(1));

        _log.Stop();
        _log.Observe(BuildPacket(2));

        Assert.That(_log.Snapshot(), Is.Empty);
    }

    [Test]
    public void A_cleared_log_forgets_its_packets_and_keeps_the_next_ones()
    {
        _log.Start();
        _log.Observe(BuildPacket(1));

        _log.Clear();
        _log.Observe(BuildPacket(2));

        Assert.That(_log.Snapshot().Select(packet => packet.Opcode), Is.EqualTo(new ushort[] { 2 }));
    }

    [Test]
    public void A_full_log_keeps_only_the_last_thousand_packets()
    {
        _log.Start();

        for (ushort opcode = 1; opcode <= 1005; opcode++)
        {
            _log.Observe(BuildPacket(opcode));
        }

        var opcodes = _log.Snapshot().Select(packet => packet.Opcode).ToArray();

        Assert.That(opcodes, Has.Length.EqualTo(1000));
        Assert.That(opcodes[0], Is.EqualTo(6));
        Assert.That(opcodes[^1], Is.EqualTo(1005));
    }

    [Test]
    public void Packets_observed_on_several_threads_at_once_are_all_kept()
    {
        _log.Start();

        Parallel.For(0, 8, _ =>
        {
            for (ushort opcode = 0; opcode < 100; opcode++)
            {
                _log.Observe(BuildPacket(opcode));
                _log.Snapshot();
            }
        });

        Assert.That(_log.Snapshot(), Has.Count.EqualTo(800).And.None.Null);
    }

    private static ObservedPacket BuildPacket(ushort opcode)
    {
        return new ObservedPacket(DateTime.Now, PacketDirection.Incoming, opcode, Array.Empty<byte>(), [],
            EmptyPacketReason.NoDecoder);
    }
}
