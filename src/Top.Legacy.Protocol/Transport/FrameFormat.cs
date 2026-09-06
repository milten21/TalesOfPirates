using System;

namespace Top.Legacy.Protocol.Transport
{
    public class FrameFormat
    {
        private readonly int _lengthOffset;
        private readonly int _lengthSize;

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

            if (lengthOffset < 0 || sessionIdSize < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lengthOffset), $"{lengthOffset} and {sessionIdSize} are not both positive.");
            }

            _lengthOffset = lengthOffset;
            _lengthSize = lengthSize;

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
    }
}
