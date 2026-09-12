using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Top.Legacy.Protocol.Packets;
using Top.Legacy.Protocol.Transport;

namespace Top.Legacy.Protocol.Tests
{
    public class LoopbackGate : IDisposable
    {
        private const int ReadTimeoutMilliseconds = 10 * 1000;

        private readonly TcpListener _listener;
        private readonly FrameFormat _frameFormat = new FrameFormat();

        private TcpClient _client;
        private NetworkStream _stream;
        private FrameWriter _frameWriter;

        public LoopbackGate()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public int Port { get; }

        public void Accept()
        {
            _client = _listener.AcceptTcpClient();
            _client.NoDelay = true;
            _stream = _client.GetStream();
            _stream.ReadTimeout = ReadTimeoutMilliseconds;
            _frameWriter = new FrameWriter(_stream, _frameFormat);
        }

        public void WritePacket(ushort opcode, Action<PacketWriter> payload = null)
        {
            var writer = new PacketWriter(opcode);
            payload?.Invoke(writer);
            WritePacket(writer.ToArray());
        }

        public void WritePacket(byte[] packet)
        {
            _frameWriter.WritePacket(packet);
        }

        public byte[] ReadFrame()
        {
            var head = new byte[_frameFormat.IdleFrameLength];
            Fill(head, 0, head.Length);
            var length = _frameFormat.ReadLength(head);

            if (length == _frameFormat.IdleFrameLength)
            {
                return null;
            }

            var rest = new byte[length - head.Length];
            Fill(rest, 0, rest.Length);

            var packet = new byte[length - _frameFormat.HeaderLength];
            Array.Copy(rest, _frameFormat.HeaderLength - head.Length, packet, 0, packet.Length);

            return packet;
        }

        public byte[] ReadPacketBytes()
        {
            byte[] packet;

            do
            {
                packet = ReadFrame();
            }
            while (packet == null);

            return packet;
        }

        public PacketReader ReadPacket()
        {
            return new PacketReader(ReadPacketBytes());
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _client?.Dispose();
            _listener.Stop();
        }

        private void Fill(byte[] buffer, int at, int count)
        {
            while (count > 0)
            {
                var read = _stream.Read(buffer, at, count);

                if (read <= 0)
                {
                    throw new EndOfStreamException("The client stream ended.");
                }

                at += read;
                count -= read;
            }
        }
    }
}
