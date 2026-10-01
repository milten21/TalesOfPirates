using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class CharacterBeginSeeTests
    {
        private const string SeenNew = "00";

        private const string CastleGuard =
            "00000005B00028CAB00028CA0001000004000003020015436173746C65204775617264202D20506574657200000100000000" +
            "00000000010000010000000000000100000100035854000438DC00000028005A000000000000B00028CA0100000001000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000";

        private const string CastleGuardNpcShow =
            "0001";

        private const string Standing =
            "0000";

        private const string SeatedFacing270InPose12 =
            "0002" + "010E" + "000C";

        private const string LeaningInState1Pose3 =
            "0001" + "01" + "00000003" + "0000005A" + "00035854" + "000438DC" + "00000064";

        private const string Attributes =
            "000005000000012C010000149B1F0000149B2A000004C12C00000000";

        private const string SkillStates =
            "017C1CB700";

        private const string PinnedAppearHex =
            SeenNew + CastleGuard + CastleGuardNpcShow + Standing + Attributes + SkillStates;

        private const string Observer =
            "000000010000000400000004000A4F6273657276657232006302002E2E01000A4F6273657276657232000001000001000000" +
            "000001000001000000000000010000010003572D00043F0300000028013B0000000001000000000401000000010000000100" +
            "07D0000000000000000009FA0000000000000000000000000000000000010000000000010000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000";

        private const string NoNpcShow =
            "0000";

        private const string PlayerAttributes =
            "00004A0000000001010000006402000000170300000000040000000005000000000600000004070000000008000000000900" +
            "0000000A000000010B000000010C000000010D000000000E000000000F000000001000000005110000000012000000001300" +
            "000000140000000015000002711600000000170000000018000005DC19000000051A000000051B000000051C000000051D00" +
            "0000051E000000051F000000642000000017210000000722000000072300000002240000000A250000000A26000000692700" +
            "00000F280000000229000000012A000005942B000000002C000001E02D000000002E000000002F0000000030000000003100" +
            "000000320000000533000000053400000005350000000536000000053700000005380000006439000000173A000000073B00" +
            "0000073C000000023D0000000A3E0000000A3F00000069400000000F41000000024200000001430000059444000000004500" +
            "0001E04600000000470000000048000000004900000000";

        private const string ObserverSkillStates =
            "0189A2A100";

        private const string PinnedPlayerAppearHex =
            SeenNew + Observer + NoNpcShow + Standing + PlayerAttributes + ObserverSkillStates;

        private const byte Player = 1;
        private const byte Npc = 2;
        private const byte LevelAttribute = 0;
        private const byte HealthAttribute = 1;
        private const byte MaxHealthAttribute = 31;

        [Test]
        public void A_pinned_appear_names_the_character_that_came_into_view()
        {
            var appeared = Read(PinnedAppearHex);

            Assert.That(appeared.SeeType, Is.Zero, "a new character");
            Assert.That(appeared.Actor.Name, Is.EqualTo("Castle Guard - Peter"));
            Assert.That(appeared.Actor.ControlType, Is.EqualTo(Npc));
            Assert.That(appeared.Actor.CharacterId, Is.EqualTo(5));
            Assert.That(appeared.Actor.WorldId, Is.EqualTo(0xB00028CA));
            Assert.That(appeared.Actor.Position, Is.EqualTo(new Point(219220, 276700)));
            Assert.That(appeared.Actor.Angle, Is.EqualTo(90));
        }

        [Test]
        public void A_character_that_is_not_a_player_carries_five_attributes_with_the_level_and_the_health_among_them()
        {
            var attributes = Read(PinnedAppearHex).Attributes;

            Assert.That(attributes, Has.Count.EqualTo(5));
            Assert.That(FindValue(attributes, LevelAttribute), Is.EqualTo(300));
            Assert.That(FindValue(attributes, HealthAttribute), Is.EqualTo(5275));
            Assert.That(FindValue(attributes, MaxHealthAttribute), Is.EqualTo(5275));
        }

        [Test]
        public void A_player_carries_seventy_four_attributes_and_still_ends_on_its_last_byte()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedPlayerAppearHex));

            var appeared = CharacterBeginSee.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(appeared.Actor.Name, Is.EqualTo("Observer2"));
            Assert.That(appeared.Actor.ControlType, Is.EqualTo(Player));
            Assert.That(appeared.Actor.WorldId, Is.EqualTo(4));
            Assert.That(appeared.Actor.Position, Is.EqualTo(new Point(218925, 278275)));
            Assert.That(appeared.Actor.Angle, Is.EqualTo(315));
            Assert.That(appeared.Attributes, Has.Count.EqualTo(74));
            Assert.That(FindValue(appeared.Attributes, LevelAttribute), Is.EqualTo(1));
            Assert.That(FindValue(appeared.Attributes, HealthAttribute), Is.EqualTo(100));
            Assert.That(FindValue(appeared.Attributes, MaxHealthAttribute), Is.EqualTo(100));
        }

        [Test]
        public void A_pinned_appear_ends_on_its_last_byte()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedAppearHex));

            var appeared = CharacterBeginSee.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(appeared.SkillStates, Is.Empty);
        }

        [Test]
        public void A_pinned_appear_writes_back_the_bytes_the_server_sent()
        {
            var packet = new PacketWriter();
            Read(PinnedAppearHex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedAppearHex)));
        }

        [Test]
        public void A_seated_character_carries_its_facing_and_its_pose()
        {
            var packet = new PacketReader(Hex.Bytes(
                SeenNew + CastleGuard + CastleGuardNpcShow + SeatedFacing270InPose12 + Attributes + SkillStates));

            var appeared = CharacterBeginSee.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(appeared.PoseState, Is.EqualTo(CharacterBeginSee.PoseSeat));
            Assert.That(appeared.Seat.Angle, Is.EqualTo(270));
            Assert.That(appeared.Seat.Pose, Is.EqualTo(12));
            Assert.That(appeared.Lean, Is.Null);
            Assert.That(appeared.Attributes, Has.Count.EqualTo(5));
        }

        [Test]
        public void A_leaning_character_carries_where_and_how_it_leans()
        {
            var packet = new PacketReader(Hex.Bytes(
                SeenNew + CastleGuard + CastleGuardNpcShow + LeaningInState1Pose3 + Attributes + SkillStates));

            var appeared = CharacterBeginSee.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(appeared.PoseState, Is.EqualTo(CharacterBeginSee.PoseLean));
            Assert.That(appeared.Lean.State, Is.EqualTo(1));
            Assert.That(appeared.Lean.Pose, Is.EqualTo(3));
            Assert.That(appeared.Lean.Angle, Is.EqualTo(90));
            Assert.That(appeared.Lean.Position, Is.EqualTo(new Point(219220, 276700)));
            Assert.That(appeared.Lean.Height, Is.EqualTo(100));
            Assert.That(appeared.Seat, Is.Null);
            Assert.That(appeared.Attributes, Has.Count.EqualTo(5));
        }

        [TestCase(SeatedFacing270InPose12)]
        [TestCase(LeaningInState1Pose3)]
        public void A_posed_character_writes_back_the_bytes_it_was_read_from(string pose)
        {
            var hex = SeenNew + CastleGuard + CastleGuardNpcShow + pose + Attributes + SkillStates;
            var packet = new PacketWriter();

            Read(hex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(hex)));
        }

        [Test]
        public void An_appear_that_runs_out_of_bytes_throws()
        {
            var pinned = Hex.Bytes(PinnedAppearHex);
            var truncated = new byte[pinned.Length - 1];
            Array.Copy(pinned, truncated, truncated.Length);

            Assert.That(
                () => CharacterBeginSee.Read(new PacketReader(truncated)),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_pose_block_that_does_not_match_its_pose_state_is_refused()
        {
            var pinned = Read(PinnedAppearHex);

            Assert.That(
                () => new CharacterBeginSee(pinned.SeeType, pinned.Actor, pinned.NpcShow, CharacterBeginSee.PoseSeat,
                    null, null, pinned.AttributeType, pinned.Attributes, pinned.ServerTime, pinned.SkillStates),
                Throws.ArgumentException);
        }

        private static CharacterBeginSee Read(string hex)
        {
            return CharacterBeginSee.Read(new PacketReader(Hex.Bytes(hex)));
        }

        private static long FindValue(IReadOnlyList<CharacterAttribute> attributes, byte id)
        {
            return attributes.Single(attribute => attribute.Id == id).Value;
        }
    }
}
