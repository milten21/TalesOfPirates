using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Login
{
    public class LoginReply
    {
        private const ushort SuccessCode = 0;

        private static readonly CharacterSummary[] NoCharacters = Array.Empty<CharacterSummary>();

        private LoginReply(
            ushort code,
            string refusalText,
            byte slotCap,
            IReadOnlyList<CharacterSummary> characters,
            bool hasSecondPassword)
        {
            Code = code;
            RefusalText = refusalText;
            SlotCap = slotCap;
            Characters = characters;
            HasSecondPassword = hasSecondPassword;
        }

        public ushort Code { get; }

        public string RefusalText { get; }

        public byte SlotCap { get; }

        public IReadOnlyList<CharacterSummary> Characters { get; }

        public bool HasSecondPassword { get; }

        public bool IsRefused => Code != SuccessCode;

        public static LoginReply Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var code = packet.ReadUShort();

            if (code != SuccessCode)
            {
                return Refused(code, ReadRefusalText(packet));
            }

            var slotCap = packet.ReadByte();
            var characters = CharacterSummary.ReadList(packet);

            return Accepted(slotCap, characters, packet.ReadByte() != 0);
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
                packet.WriteString(RefusalText);

                return;
            }

            packet.WriteByte(SlotCap);
            CharacterSummary.WriteList(packet, Characters);
            packet.WriteByte(HasSecondPassword ? (byte)1 : (byte)0);
        }

        private static string ReadRefusalText(PacketReader packet)
        {
            if (packet.Remaining < sizeof(ushort))
            {
                return string.Empty;
            }

            return packet.PeekUShort() == packet.Remaining - sizeof(ushort) ? packet.ReadString() : string.Empty;
        }

        public static LoginReply Accepted(byte slotCap, IReadOnlyList<CharacterSummary> characters,
            bool hasSecondPassword)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            return new LoginReply(SuccessCode, string.Empty, slotCap, characters, hasSecondPassword);
        }

        public static LoginReply Refused(ushort code, string refusalText)
        {
            if (code == SuccessCode)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(code), code, "A refused login carries a code above zero.");
            }

            return new LoginReply(code, refusalText ?? string.Empty, 0, NoCharacters, false);
        }
    }
}
