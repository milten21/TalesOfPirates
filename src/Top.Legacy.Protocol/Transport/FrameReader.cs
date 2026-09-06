using System;
using System.IO;

namespace Top.Legacy.Protocol.Transport
{
    public class FrameReader
    {
        private readonly Stream _stream;
        private readonly FrameFormat _frameFormat;
        private readonly byte[] _lengthField;

        public FrameReader(Stream stream, FrameFormat frameFormat = null)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _frameFormat = frameFormat ?? new FrameFormat();
            _lengthField = new byte[_frameFormat.IdleFrameLength];
        }

        public byte[] ReadPacket()
        {
            while (true)
            {
                Fill(_lengthField, 0, _lengthField.Length);

                var length = _frameFormat.ReadLength(_lengthField);

                if (length == _frameFormat.IdleFrameLength)
                {
                    continue;
                }

                if (length < _frameFormat.HeaderLength + sizeof(ushort) || length > _frameFormat.MaxReceiveLength)
                {
                    throw new InvalidDataException($"A frame of {length} bytes is neither idle nor a packet.");
                }

                var frame = new byte[length];
                Array.Copy(_lengthField, frame, _lengthField.Length);
                Fill(frame, _lengthField.Length, length - _lengthField.Length);

                var packet = new byte[length - _frameFormat.HeaderLength];
                Array.Copy(frame, _frameFormat.HeaderLength, packet, 0, packet.Length);

                return packet;
            }
        }

        private void Fill(byte[] buffer, int at, int count)
        {
            while (count > 0)
            {
                var read = _stream.Read(buffer, at, count);

                if (read <= 0)
                {
                    throw new EndOfStreamException("The stream ended.");
                }

                at += read;
                count -= read;
            }
        }
    }
}
