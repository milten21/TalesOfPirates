using System;

namespace Top.Legacy.Protocol.Transport
{
    public class FrameFormat
    {
        private const uint PlainPacketSessionId = 0x80000000;

        private readonly int _lengthOffset;
        private readonly int _lengthSize;
        private readonly int _sessionIdSize;

        public FrameFormat(
            int lengthOffset = 0,
            int lengthSize = 2,
            int sessionIdSize = 4,
            int maxSendLength = 16 * 1024,
            int maxReceiveLength = 64 * 1024)
        {
            if (lengthSize != 1 && lengthSize != 2 && lengthSize != 4)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lengthSize), lengthSize, "A total length is 1, 2 or 4 bytes wide.");
            }

            if (lengthOffset < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lengthOffset), lengthOffset, "A length offset is zero or more.");
            }

            if (sessionIdSize != 0 && sessionIdSize != sizeof(uint))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sessionIdSize), sessionIdSize, "A session id is absent or four bytes wide.");
            }

            _lengthOffset = lengthOffset;
            _lengthSize = lengthSize;
            _sessionIdSize = sessionIdSize;

            IdleFrameLength = lengthOffset + lengthSize;
            HeaderLength = IdleFrameLength + sessionIdSize;

            var largest = lengthSize == sizeof(uint) ? int.MaxValue : (1 << (lengthSize * 8)) - 1;
            MaxSendLength = Math.Min(maxSendLength, largest);
            MaxReceiveLength = Math.Min(maxReceiveLength, largest);
            MaxSendPacketLength = MaxSendLength - HeaderLength;
        }

        public int IdleFrameLength { get; }

        public int HeaderLength { get; }

        public int MaxSendLength { get; }

        public int MaxSendPacketLength { get; }

        public int MaxReceiveLength { get; }

        public int ReadLength(byte[] frame)
        {
            var length = 0;

            for (var i = 0; i < _lengthSize; i++)
            {
                length = (length << 8) | frame[_lengthOffset + i];
            }

            return length;
        }

        public void WriteLength(byte[] frame, int length)
        {
            for (var i = 0; i < _lengthSize; i++)
            {
                frame[_lengthOffset + i] = (byte)(length >> ((_lengthSize - 1 - i) * 8));
            }
        }

        public void WriteHeader(byte[] frame, int length)
        {
            WriteLength(frame, length);

            for (var i = 0; i < _sessionIdSize; i++)
            {
                frame[IdleFrameLength + i] = (byte)(PlainPacketSessionId >> ((_sessionIdSize - 1 - i) * 8));
            }
        }
    }
}
