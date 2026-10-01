using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class BeginPlayRequest
    {
        public BeginPlayRequest(byte characterIndex)
        {
            CharacterIndex = characterIndex;
        }

        public byte CharacterIndex { get; }

        public static BeginPlayRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new BeginPlayRequest(packet.ReadByte());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(CharacterIndex);
        }
    }
}
