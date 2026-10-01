using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class DeleteCharacterRequest
    {
        public DeleteCharacterRequest(byte characterIndex, string code)
        {
            CharacterIndex = characterIndex;
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        public byte CharacterIndex { get; }

        public string Code { get; }

        public static DeleteCharacterRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var characterIndex = packet.ReadByte();

            return new DeleteCharacterRequest(characterIndex, packet.ReadString());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(CharacterIndex);
            packet.WriteString(Code);
        }
    }
}
