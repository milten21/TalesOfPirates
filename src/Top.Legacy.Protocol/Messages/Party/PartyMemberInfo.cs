using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyMemberInfo
    {
        public PartyMemberInfo(uint characterId, string name, string motto, ushort icon)
        {
            CharacterId = characterId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Motto = motto ?? throw new ArgumentNullException(nameof(motto));
            Icon = icon;
        }

        public uint CharacterId { get; }

        public string Name { get; }

        public string Motto { get; }

        public ushort Icon { get; }

        public static PartyMemberInfo Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var characterId = packet.ReadUInt();
            var name = packet.ReadString();
            var motto = packet.ReadString();

            return new PartyMemberInfo(characterId, name, motto, packet.ReadUShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(CharacterId);
            packet.WriteString(Name);
            packet.WriteString(Motto);
            packet.WriteUShort(Icon);
        }
    }
}
