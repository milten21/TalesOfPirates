using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class CharacterBeginSee
    {
        public const short PoseLean = 1;
        public const short PoseSeat = 2;

        public CharacterBeginSee(byte seeType, ActorCreate actor, NpcShow npcShow, short poseState, LeanInfo lean,
            Face seat, byte attributeType, IReadOnlyList<CharacterAttribute> attributes, int serverTime,
            IReadOnlyList<SkillState> skillStates)
        {
            if ((poseState == PoseLean) != (lean != null))
            {
                throw new ArgumentException($"A lean is carried with pose state {PoseLean} only.", nameof(lean));
            }

            if ((poseState == PoseSeat) != (seat != null))
            {
                throw new ArgumentException($"A seat is carried with pose state {PoseSeat} only.", nameof(seat));
            }

            SeeType = seeType;
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            NpcShow = npcShow ?? throw new ArgumentNullException(nameof(npcShow));
            PoseState = poseState;
            Lean = lean;
            Seat = seat;
            AttributeType = attributeType;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            ServerTime = serverTime;
            SkillStates = skillStates ?? throw new ArgumentNullException(nameof(skillStates));
        }

        public byte SeeType { get; }

        public ActorCreate Actor { get; }

        // TODO: missions publish the NPC show pair.
        public NpcShow NpcShow { get; }

        // TODO: animation publishes the pose block.
        public short PoseState { get; }

        // TODO: animation publishes the pose block.
        public LeanInfo Lean { get; }

        // TODO: animation publishes the pose block.
        public Face Seat { get; }

        // TODO: the world UI publishes the attribute sync type.
        public byte AttributeType { get; }

        public IReadOnlyList<CharacterAttribute> Attributes { get; }

        // TODO: the world UI publishes the server time to time the skill states.
        public int ServerTime { get; }

        // TODO: the world UI publishes the skill states.
        public IReadOnlyList<SkillState> SkillStates { get; }

        public static CharacterBeginSee Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var seeType = packet.ReadByte();
            var actor = ActorCreate.Read(packet);
            var npcShow = NpcShow.Read(packet);
            var poseState = packet.ReadShort();
            var lean = poseState == PoseLean ? LeanInfo.Read(packet) : null;
            var seat = poseState == PoseSeat ? Face.Read(packet) : null;
            var attributeType = packet.ReadByte();
            var attributes = CharacterAttribute.ReadList(packet);
            var serverTime = packet.ReadInt();

            return new CharacterBeginSee(seeType, actor, npcShow, poseState, lean, seat, attributeType, attributes,
                serverTime, SkillState.ReadList(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(SeeType);
            Actor.Write(packet);
            NpcShow.Write(packet);
            packet.WriteShort(PoseState);
            Lean?.Write(packet);
            Seat?.Write(packet);
            packet.WriteByte(AttributeType);
            CharacterAttribute.WriteList(packet, Attributes);
            packet.WriteInt(ServerTime);
            SkillState.WriteList(packet, SkillStates);
        }
    }
}
