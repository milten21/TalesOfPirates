using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Top.Legacy.Protocol.Crypto;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol.Tests
{
    public class GateConnectionTests
    {
        private const ushort ForwardedOpcode = Opcode.EnterMap;
        private const int WaitMilliseconds = 10 * 1000;
        private const int QuietMilliseconds = 200;

        private AsymmetricCipherKeyPair _gateKeys;
        private byte[] _der;

        private LoopbackGate _gate;
        private GateConnection _connection;
        private BlockingCollection<ReceivedPacket> _received;
        private BlockingCollection<CloseReason> _reasons;
        private BlockingCollection<bool> _opens;
        private ushort? _failingOpcode;
        private bool _failsOnClose;

        [OneTimeSetUp]
        public void GenerateGateKeys()
        {
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 3072, 100));
            _gateKeys = generator.GenerateKeyPair();
            _der = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_gateKeys.Public).GetDerEncoded();
        }

        [SetUp]
        public void StartGate()
        {
            _gate = new LoopbackGate();
            _received = new BlockingCollection<ReceivedPacket>();
            _reasons = new BlockingCollection<CloseReason>();
            _opens = new BlockingCollection<bool>();
            _failingOpcode = null;
            _failsOnClose = false;
        }

        [TearDown]
        public void StopGate()
        {
            _connection?.Dispose();
            _connection = null;
            _gate.Dispose();
        }

        [Test]
        public void A_sent_packet_carries_its_opcode_and_the_count_of_the_packets_before_it()
        {
            Connect(Settings());
            _connection.Send(ForwardedOpcode, writer => writer.WriteUShort(7));

            var packet = _gate.ReadPacket();

            Assert.That(packet.ReadUShort(), Is.EqualTo(ForwardedOpcode));
            Assert.That(packet.ReadUInt(), Is.Zero);
            Assert.That(packet.ReadUShort(), Is.EqualTo(7));
        }

        [Test]
        public void A_payload_is_written_before_send_returns()
        {
            Connect(Settings());
            var written = false;

            _connection.Send(ForwardedOpcode, writer => written = true);

            Assert.That(written, Is.True);
        }

        [Test]
        public void The_packet_count_rises_by_one_for_each_packet()
        {
            Connect(Settings());
            _connection.Send(ForwardedOpcode, null);
            _connection.Send(ForwardedOpcode, null);
            _connection.Send(ForwardedOpcode, null);

            Assert.That(new[] { CountOf(_gate.ReadPacket()), CountOf(_gate.ReadPacket()), CountOf(_gate.ReadPacket()) },
                Is.EqualTo(new uint[] { 0, 1, 2 }));
        }

        [Test]
        public void A_packet_the_gate_cannot_read_is_dropped_and_spends_no_count()
        {
            Connect(Settings());
            _connection.Send(ForwardedOpcode, writer => writer.WriteRaw(new byte[16 * 1024]));
            _connection.Send(ForwardedOpcode, writer => writer.WriteUShort(7));

            var packet = _gate.ReadPacket();

            Assert.That(packet.ReadUShort(), Is.EqualTo(ForwardedOpcode));
            Assert.That(packet.ReadUInt(), Is.Zero);
            Assert.That(packet.ReadUShort(), Is.EqualTo(7));
            Assert.That(_reasons.TryTake(out _, QuietMilliseconds), Is.False);
        }

        [Test]
        public void A_packet_the_gate_sends_reaches_the_received_callback()
        {
            Connect(Settings());
            _gate.WritePacket(ForwardedOpcode, writer => writer.WriteString("garner"));

            var received = TakePacket();

            Assert.That(received.Opcode, Is.EqualTo(ForwardedOpcode));
            Assert.That(received.Packet.ReadString(), Is.EqualTo("garner"));
        }

        [Test]
        public void The_public_key_is_answered_with_the_key_the_gate_unwraps()
        {
            Connect(Settings(), isEncrypted: true);
            WritePublicKey();

            var packet = _gate.ReadPacket();

            Assert.That(packet.ReadUShort(), Is.EqualTo(Opcode.PrivateKey));
            Assert.That(packet.ReadUInt(), Is.Zero);
            Assert.That(Unwrap(Convert.FromBase64String(packet.ReadString())), Has.Length.EqualTo(PacketCipher.KeySize));
        }

        [Test]
        public void Every_packet_after_the_key_exchange_is_encrypted()
        {
            Connect(Settings(), isEncrypted: true);
            var cipher = ExchangeKeys();

            _connection.Send(ForwardedOpcode, writer => writer.WriteUShort(7));
            var sent = new PacketReader(cipher.Decrypt(_gate.ReadPacketBytes()));

            Assert.That(sent.ReadUShort(), Is.EqualTo(ForwardedOpcode));
            Assert.That(sent.ReadUInt(), Is.EqualTo(1), "the wrapped key was the packet before it");
            Assert.That(sent.ReadUShort(), Is.EqualTo(7));
        }

        [Test]
        public void An_encrypted_packet_from_the_gate_reaches_the_received_callback_opened()
        {
            Connect(Settings(), isEncrypted: true);
            var cipher = ExchangeKeys();

            var writer = new PacketWriter(ForwardedOpcode);
            writer.WriteString("garner");
            _gate.WritePacket(cipher.Encrypt(writer.ToArray()));

            var received = TakePacket();

            Assert.That(received.Opcode, Is.EqualTo(ForwardedOpcode));
            Assert.That(received.Packet.ReadString(), Is.EqualTo("garner"));
        }

        [Test]
        public void A_first_packet_that_is_not_the_public_key_fails_the_key_exchange()
        {
            Connect(Settings(), isEncrypted: true);
            _gate.WritePacket(ForwardedOpcode, null);

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.HandshakeFailed));
        }

        [Test]
        public void The_ping_is_answered_with_the_five_fields_it_carried()
        {
            Connect(Settings());
            _gate.WritePacket(Opcode.Ping, writer =>
            {
                for (uint field = 1; field <= 5; field++)
                {
                    writer.WriteUInt(field);
                }
            });

            var reply = _gate.ReadPacket();

            Assert.That(reply.ReadUShort(), Is.EqualTo(Opcode.PingReply));
            reply.ReadUInt();
            Assert.That(
                new[] { reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt() },
                Is.EqualTo(new uint[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void A_ping_short_of_fields_is_answered_with_zeros_and_leaves_the_connection_open()
        {
            Connect(Settings());
            _gate.WritePacket(Opcode.Ping, writer => writer.WriteUInt(1));

            var reply = _gate.ReadPacket();

            Assert.That(reply.ReadUShort(), Is.EqualTo(Opcode.PingReply));
            reply.ReadUInt();
            Assert.That(
                new[] { reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt(), reply.ReadUInt() },
                Is.EqualTo(new uint[] { 1, 0, 0, 0, 0 }));
            Assert.That(_reasons.TryTake(out _, QuietMilliseconds), Is.False);
        }

        [Test]
        public void The_check_ping_is_answered_with_the_opcode_alone()
        {
            Connect(Settings());
            _gate.WritePacket(Opcode.CheckPing, writer => writer.WriteUInt(9));

            var reply = _gate.ReadPacket();

            Assert.That(reply.ReadUShort(), Is.EqualTo(Opcode.CheckPingReply));
            reply.ReadUInt();
            Assert.That(reply.Remaining, Is.Zero);
        }

        [Test]
        public void The_group_ping_is_answered_with_the_opcode_alone()
        {
            Connect(Settings());
            _gate.WritePacket(Opcode.GroupPing, null);

            var reply = _gate.ReadPacket();

            Assert.That(reply.ReadUShort(), Is.EqualTo(Opcode.GroupPingReply));
            reply.ReadUInt();
            Assert.That(reply.Remaining, Is.Zero);
        }

        [Test]
        public void A_ping_does_not_reach_the_received_callback()
        {
            Connect(Settings());
            _gate.WritePacket(Opcode.CheckPing, null);
            _gate.WritePacket(ForwardedOpcode, null);

            Assert.That(TakePacket().Opcode, Is.EqualTo(ForwardedOpcode));
        }

        [Test]
        public void An_empty_frame_arrives_when_the_idle_interval_passes_with_nothing_to_send()
        {
            Connect(Settings(idleInterval: TimeSpan.FromMilliseconds(50)));

            Assert.That(_gate.ReadFrame(), Is.Null);
        }

        [Test]
        public void A_gate_that_closes_the_socket_ends_the_connection()
        {
            Connect(Settings());
            _gate.Dispose();

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.GateClosed));
        }

        [Test]
        public void A_gate_that_sends_nothing_for_the_read_timeout_ends_the_connection()
        {
            Connect(Settings(readTimeout: TimeSpan.FromMilliseconds(300)));

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.TimedOut));
        }

        [Test]
        public void Disposing_ends_the_connection()
        {
            Connect(Settings());
            _connection.Dispose();

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.CallerClosed));
        }

        [Test]
        public void One_reason_is_reported_however_many_ways_the_connection_ends()
        {
            Connect(Settings());
            _gate.Dispose();
            TakeReason();

            _connection.Dispose();

            Assert.That(_reasons.TryTake(out _, 500), Is.False);
        }

        [Test]
        public void A_received_handler_that_fails_leaves_the_connection_open()
        {
            _failingOpcode = ForwardedOpcode;
            Connect(Settings());
            _gate.WritePacket(ForwardedOpcode, null);
            _gate.WritePacket(Opcode.LoginReply, null);

            Assert.That(TakePacket().Opcode, Is.EqualTo(Opcode.LoginReply));
            Assert.That(_reasons.TryTake(out _, QuietMilliseconds), Is.False);
        }

        [Test]
        public void A_close_handler_that_fails_does_not_reach_the_caller()
        {
            _failsOnClose = true;
            Connect(Settings());

            Assert.DoesNotThrow(() => _connection.Dispose());
            Assert.That(TakeReason(), Is.EqualTo(CloseReason.CallerClosed));
        }

        [Test]
        public void An_open_connection_reaches_the_opened_callback_once()
        {
            Connect(Settings());

            Assert.That(_opens.TryTake(out _, WaitMilliseconds), Is.True, "the connection never opened");
            Assert.That(_opens.TryTake(out _, QuietMilliseconds), Is.False);
        }

        [Test]
        public void An_encrypted_connection_opens_only_after_the_key_exchange()
        {
            Connect(Settings(), isEncrypted: true);

            Assert.That(_opens.TryTake(out _, QuietMilliseconds), Is.False, "the connection opened before the key exchange");

            ExchangeKeys();

            Assert.That(_opens.TryTake(out _, WaitMilliseconds), Is.True, "the connection never opened");
        }

        [Test]
        public void A_failed_key_exchange_never_opens()
        {
            Connect(Settings(), isEncrypted: true);
            _gate.WritePacket(ForwardedOpcode, null);

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.HandshakeFailed));
            Assert.That(_opens.TryTake(out _, QuietMilliseconds), Is.False);
        }

        [Test]
        public void A_second_open_is_refused()
        {
            Connect(Settings());

            Assert.Throws<InvalidOperationException>(() => _connection.Open(new RecordingListener(this)));
        }

        [Test]
        public void An_open_after_the_connection_ended_is_refused()
        {
            _connection = Build(_gate.Port, Settings());
            _connection.Dispose();

            Assert.Throws<InvalidOperationException>(() => _connection.Open(new RecordingListener(this)));
        }

        [Test]
        public void A_gate_that_does_not_listen_is_unreachable()
        {
            int port;

            using (var quiet = new LoopbackGate())
            {
                port = quiet.Port;
            }

            _connection = Build(port, Settings());
            _connection.Open(new RecordingListener(this));

            Assert.That(TakeReason(), Is.EqualTo(CloseReason.Unreachable));
            Assert.That(_opens.TryTake(out _, QuietMilliseconds), Is.False);
        }

        private static GateSettings Settings(TimeSpan? idleInterval = null, TimeSpan? readTimeout = null)
        {
            return new GateSettings(idleInterval: idleInterval, readTimeout: readTimeout);
        }

        private static uint CountOf(PacketReader packet)
        {
            packet.ReadUShort();

            return packet.ReadUInt();
        }

        private GateConnection Build(int port, GateSettings settings, bool isEncrypted = false)
        {
            return new GateConnection("127.0.0.1", port, isEncrypted, settings);
        }

        private void Connect(GateSettings settings, bool isEncrypted = false)
        {
            _connection = Build(_gate.Port, settings, isEncrypted);
            _connection.Open(new RecordingListener(this));
            _gate.Accept();
        }

        private void WritePublicKey()
        {
            _gate.WritePacket(Opcode.ServerPublicKey, writer =>
            {
                writer.WriteUShort((ushort)_der.Length);
                writer.WriteSequence(_der);
            });
        }

        private PacketCipher ExchangeKeys()
        {
            WritePublicKey();

            var packet = _gate.ReadPacket();
            packet.ReadUShort();
            packet.ReadUInt();

            return new PacketCipher(Unwrap(Convert.FromBase64String(packet.ReadString())));
        }

        private ReceivedPacket TakePacket()
        {
            Assert.That(_received.TryTake(out var packet, WaitMilliseconds), Is.True, "no packet was received");

            return packet;
        }

        private CloseReason TakeReason()
        {
            Assert.That(_reasons.TryTake(out var reason, WaitMilliseconds), Is.True, "the connection reported no reason");

            return reason;
        }

        private byte[] Unwrap(byte[] wrapped)
        {
            var oaep = new OaepEncoding(new RsaEngine(), new Sha1Digest());
            oaep.Init(false, _gateKeys.Private);

            return oaep.ProcessBlock(wrapped, 0, wrapped.Length);
        }

        private class RecordingListener : IGateListener
        {
            private readonly GateConnectionTests _tests;

            public RecordingListener(GateConnectionTests tests)
            {
                _tests = tests;
            }

            public void OnOpened()
            {
                _tests._opens.Add(true);
            }

            public void OnReceived(ushort opcode, PacketReader packet)
            {
                if (opcode == _tests._failingOpcode)
                {
                    throw new InvalidOperationException("the handler failed");
                }

                _tests._received.Add(new ReceivedPacket(opcode, packet));
            }

            public void OnClosed(CloseReason reason)
            {
                _tests._reasons.Add(reason);

                if (_tests._failsOnClose)
                {
                    throw new InvalidOperationException("the close handler failed");
                }
            }
        }

        private class ReceivedPacket
        {
            public ReceivedPacket(ushort opcode, PacketReader packet)
            {
                Opcode = opcode;
                Packet = packet;
            }

            public ushort Opcode { get; }

            public PacketReader Packet { get; }
        }
    }
}
