using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class Boat
    {
        public Boat(ActorCreate actor, byte attributeType, IReadOnlyList<CharacterAttribute> attributes,
            KitbagSync kitbag, int serverTime, IReadOnlyList<SkillState> skillStates)
        {
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            AttributeType = attributeType;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Kitbag = kitbag ?? throw new ArgumentNullException(nameof(kitbag));
            ServerTime = serverTime;
            SkillStates = skillStates ?? throw new ArgumentNullException(nameof(skillStates));
        }

        public ActorCreate Actor { get; }

        public byte AttributeType { get; }

        public IReadOnlyList<CharacterAttribute> Attributes { get; }

        public KitbagSync Kitbag { get; }

        public int ServerTime { get; }

        public IReadOnlyList<SkillState> SkillStates { get; }

        public static Boat Read(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var actor = ActorCreate.Read(packet);
            var attributeType = packet.ReadByte();
            var attributes = CharacterAttribute.ReadList(packet);
            var kitbag = KitbagSync.Read(packet, boatItemIds);
            var serverTime = packet.ReadInt();

            return new Boat(actor, attributeType, attributes, kitbag, serverTime, SkillState.ReadList(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            Actor.Write(packet);
            packet.WriteByte(AttributeType);
            CharacterAttribute.WriteList(packet, Attributes);
            Kitbag.Write(packet);
            packet.WriteInt(ServerTime);
            SkillState.WriteList(packet, SkillStates);
        }
    }
}
