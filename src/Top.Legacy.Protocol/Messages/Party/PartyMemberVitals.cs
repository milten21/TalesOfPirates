using System;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyMemberVitals
    {
        public PartyMemberVitals(uint memberId, int health, int maxHealth, int mana, int maxMana, int level,
            LookInfo look)
        {
            MemberId = memberId;
            Health = health;
            MaxHealth = maxHealth;
            Mana = mana;
            MaxMana = maxMana;
            Level = level;
            Look = look ?? throw new ArgumentNullException(nameof(look));
        }

        public uint MemberId { get; }

        public int Health { get; }

        public int MaxHealth { get; }

        public int Mana { get; }

        public int MaxMana { get; }

        public int Level { get; }

        public LookInfo Look { get; }

        public static PartyMemberVitals Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var memberId = packet.ReadUInt();
            var health = packet.ReadInt();
            var maxHealth = packet.ReadInt();
            var mana = packet.ReadInt();
            var maxMana = packet.ReadInt();
            var level = packet.ReadInt();

            return new PartyMemberVitals(memberId, health, maxHealth, mana, maxMana, level, LookInfo.Read(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(MemberId);
            packet.WriteInt(Health);
            packet.WriteInt(MaxHealth);
            packet.WriteInt(Mana);
            packet.WriteInt(MaxMana);
            packet.WriteInt(Level);
            Look.Write(packet);
        }
    }
}
