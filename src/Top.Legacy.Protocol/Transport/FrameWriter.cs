using System;
using System.IO;

namespace Top.Legacy.Protocol.Transport
{
    public class FrameWriter
    {
        private readonly Stream _stream;
        private readonly FrameFormat _frameFormat;

        public FrameWriter(Stream stream, FrameFormat frameFormat = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _frameFormat = frameFormat ?? new FrameFormat();
        }

        public void WritePacket(byte[] packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (packet.Length > _frameFormat.MaxSendPacketLength)
            {
                throw new InvalidDataException(
                    $"A packet of {packet.Length} bytes passes the {_frameFormat.MaxSendPacketLength} bytes the peer reads.");
            }

            var length = _frameFormat.HeaderLength + packet.Length;
            var frame = new byte[length];
            _frameFormat.WriteLength(frame, length);
            Array.Copy(packet, 0, frame, _frameFormat.HeaderLength, packet.Length);
            WriteFrame(frame);
        }

        public void WriteIdleFrame()
        {
            var frame = new byte[_frameFormat.IdleFrameLength];
            _frameFormat.WriteLength(frame, _frameFormat.IdleFrameLength);
            WriteFrame(frame);
        }

        private void WriteFrame(byte[] frame)
        {
            _stream.Write(frame, 0, frame.Length);
            _stream.Flush();
        }
    }
}
