using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class EndPlayReply
    {
        private const ushort SuccessCode = 0;

        private static readonly CharacterSummary[] NoCharacters = Array.Empty<CharacterSummary>();

        private EndPlayReply(ushort code, byte slotCap, IReadOnlyList<CharacterSummary> characters)
        {
            Code = code;
            SlotCap = slotCap;
            Characters = characters;
        }

        public ushort Code { get; }

        public byte SlotCap { get; }

        public IReadOnlyList<CharacterSummary> Characters { get; }

        public bool IsRefused => Code != SuccessCode;

        public static EndPlayReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = packet.ReadUShort();

            if (code != SuccessCode)
            {
                return Refused(code);
            }

            var slotCap = packet.ReadByte();

            return Accepted(slotCap, CharacterSummary.ReadList(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUShort(Code);

            if (IsRefused)
            {
                return;
            }

            packet.WriteByte(SlotCap);
            CharacterSummary.WriteList(packet, Characters);
        }

        public static EndPlayReply Accepted(byte slotCap, IReadOnlyList<CharacterSummary> characters)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            return new EndPlayReply(SuccessCode, slotCap, characters);
        }

        public static EndPlayReply Refused(ushort code)
        {
            if (code == SuccessCode)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(code), code, "A refused end play carries a code above zero.");
            }

            return new EndPlayReply(code, 0, NoCharacters);
        }
    }
}
