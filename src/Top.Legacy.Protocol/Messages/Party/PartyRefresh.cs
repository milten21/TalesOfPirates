using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyRefresh
    {
        public const byte Join = 1;
        public const byte Leave = 2;
        public const byte Kick = 6;

        public PartyRefresh(byte kind, IReadOnlyList<PartyMemberInfo> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            if (members.Count > byte.MaxValue)
            {
                throw new ArgumentException(
                    $"A party refresh counts its members in one byte and {members.Count} were given.",
                    nameof(members));
            }

            Kind = kind;
            Members = members;
        }

        public byte Kind { get; }

        public IReadOnlyList<PartyMemberInfo> Members { get; }

        public static PartyRefresh Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var kind = packet.ReadByte();
            var members = new PartyMemberInfo[packet.ReadByte()];

            for (var member = 0; member < members.Length; member++)
            {
                members[member] = PartyMemberInfo.Read(packet);
            }

            return new PartyRefresh(kind, members);
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(Kind);
            packet.WriteByte((byte)Members.Count);

            foreach (var member in Members)
            {
                member.Write(packet);
            }
        }
    }
}
