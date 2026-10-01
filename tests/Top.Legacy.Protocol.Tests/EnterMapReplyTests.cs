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
    public class EnterMapReplyTests
    {
        private const string Header =
            "00000000010000076761726E6572000100000000";

        private const string Actor =
            "000000010000000100000001000B54657374506972617465000002002E2D01000B5465737450697261746500000100000100" +
            "0000000001000001000000000000010000010002E735000458D0000000280167000000000100000000010100000001000000" +
            "010007D0000000000000000009FA00000000000000000000000000000000000100000000000100000000000000000010D400" +
            "00000000000001271027100000000000010100000000013B9ACA00000000000100240004002F000100000000000000000000" +
            "0000000000000000000002810000000000000001271027100000000000010100000000010000000000000000010027000100" +
            "2400020000000000000000000000000000000000000000270F00000000000000000000000000000000000000000000000100" +
            "00000000000000000000000000000000000000000000000000090000000000000001270E2710000000000001010000000001" +
            "3B9ACA0000000000010022001D00230021000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "000000000000000000000000000000000000000000000000000000000000";

        private const string SkillBag =
            "001C00000900190001000000000000000000000000001C0101000000000000000000000000001D0001000000000000000000" +
            "000000001E0001000000000000000000000000001F0001000000000000000000000000002200010000000000000000000000" +
            "00002300010000000000000000000000000026000100000000000000000000000001DB0101000700000000000027100000";

        private const string SkillStates =
            "61B915B700";

        private const string Attributes =
            "00004A000000000C010000010902000000380300000000040000000005000000000600000008070000000308000009C40900" +
            "0000000A000000010B000000010C000000010D000000000E000000000F00001E0C1000002A5C1100001D2C12000000001300" +
            "000000140000000015000002711600000000170000000018000005DC19000000101A000000051B000000051C000000051D00" +
            "0000051E000000051F0000010920000000382100000038220000003C23000000082400000020250000002126000000692700" +
            "00000F280000000429000000012A000005942B000000002C000001E02D000000002E000000012F0000000030000000003100" +
            "000000320000001033000000053400000005350000000536000000053700000005380000010939000000383A0000001B3B00" +
            "00001B3C000000023D000000203E000000203F00000069400000000F41000000044200000001430000059444000000004500" +
            "0001E04600000000470000000048000000004900000000";

        private const string Kitbag =
            "000018000000080000000000000001270D271000000000000101000000000000000000000000010022000E00230012000000" +
            "000000000000000000000125950000000000000001000000000000000000010100000000000000000000000000000203E800" +
            "00000000000005000000000000000000010100000000000000000000000000000301B5000000000000000100000000000000" +
            "0000010100000000000000000000000000000401210000000000000001270F27100000000000010100000000000000000000" +
            "00000100240002002F0001000000000000000000000000000501210000000000000001271027100000000000010100000000" +
            "00000000000000000100240002002F0001000000000000000000000000000601BC0000000000000001000000000000000000" +
            "01010000000000000000000000000000070F6E00000000000000010000000000000000000101000000000000000000000000" +
            "000008000000090000000A0000000B0000000C0000000D0000000E0000000F00000010000000110000001200000013000000" +
            "140000001500000016000000170000FFFF";

        private const string Shortcuts =
            "0201DB0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
            "0000000000000000";

        private const string NoBoats =
            "00";

        private const string MainCharacterId =
            "00000001";

        private const string PinnedReplyHex =
            Header + Actor + SkillBag + SkillStates + Attributes + Kitbag + Shortcuts + NoBoats + MainCharacterId;

        private const ushort EnterFailed = 8;
        private const short VesselDeed = 3988;
        private const byte LevelAttribute = 0;
        private const byte HealthAttribute = 1;
        private const byte MaxHealthAttribute = 31;

        [Test]
        public void A_pinned_reply_names_the_map_and_the_character_standing_on_it()
        {
            var reply = Read(PinnedReplyHex);

            Assert.That(reply.IsRefused, Is.False);
            Assert.That(reply.SwitchMap.MapName, Is.EqualTo("garner"));
            Assert.That(reply.Actor.WorldId, Is.EqualTo(1));
            Assert.That(reply.Actor.Name, Is.EqualTo("TestPirate"));
            Assert.That(reply.Actor.Position, Is.EqualTo(new Point(190261, 284880)));
            Assert.That(reply.Actor.Angle, Is.EqualTo(359));
        }

        [Test]
        public void A_pinned_reply_decodes_the_rest_of_the_switch_map_block()
        {
            var reply = Read(PinnedReplyHex);

            Assert.That(reply.IsAutoLocked, Is.False);
            Assert.That(reply.IsKitbagLocked, Is.False);
            Assert.That(reply.SwitchMap.EnterType, Is.EqualTo(1));
            Assert.That(reply.SwitchMap.IsNewCharacter, Is.False);
            Assert.That(reply.SwitchMap.CanTeam, Is.True);
            Assert.That(reply.Imps, Is.Zero);
        }

        [Test]
        public void A_pinned_reply_decodes_who_the_character_is()
        {
            var actor = Read(PinnedReplyHex).Actor;

            Assert.That(actor.ControlType, Is.EqualTo(1), "a player");
            Assert.That(actor.Look.TypeId, Is.EqualTo(1));
            Assert.That(actor.ExistState, Is.EqualTo(1));
            Assert.That(actor.TeamLeaderId, Is.Zero);
            Assert.That(actor.GuildId, Is.Zero);
            Assert.That(actor.GuildName, Is.Empty);
            Assert.That(actor.GuildMotto, Is.Empty);
            Assert.That(actor.GuildPermission, Is.Zero);
            Assert.That(actor.Handle, Is.EqualTo(33566253));
            Assert.That(actor.Look.HairId, Is.EqualTo(2000));
            Assert.That(
                actor.Look.Links.Select(link => link.Id).Take(10),
                Is.EqualTo(new short[] { 0, 2554, 4308, 0, 641, 0, 9999, 0, 0, 9 }));
        }

        [Test]
        public void A_player_carries_seventy_four_attributes_with_the_level_and_the_health_among_them()
        {
            var attributes = Read(PinnedReplyHex).Attributes;

            Assert.That(attributes, Has.Count.EqualTo(74));
            Assert.That(FindValue(attributes, LevelAttribute), Is.EqualTo(12));
            Assert.That(FindValue(attributes, HealthAttribute), Is.EqualTo(265));
            Assert.That(FindValue(attributes, MaxHealthAttribute), Is.EqualTo(265));
        }

        [Test]
        public void A_pinned_reply_decodes_the_skill_bag_the_kitbag_and_the_shortcut_bar()
        {
            var reply = Read(PinnedReplyHex);

            Assert.That(reply.DefaultSkillId, Is.EqualTo(28));
            Assert.That(
                reply.SkillBag.Select(slot => slot.Id),
                Is.EqualTo(new short[] { 25, 28, 29, 30, 31, 34, 35, 38, 475 }));
            Assert.That(reply.SkillBag[8].ResumeTime, Is.EqualTo(10000));
            Assert.That(reply.SkillStates, Is.Empty);
            Assert.That(reply.Kitbag.Capacity, Is.EqualTo(24));
            Assert.That(reply.Kitbag.Slots, Has.Count.EqualTo(24));
            Assert.That(reply.Kitbag.Slots[0].Content.Id, Is.EqualTo(8));
            Assert.That(reply.Kitbag.Slots[0].Content.InstanceAttributes[0].Id, Is.EqualTo(34));
            Assert.That(reply.Kitbag.Slots[0].Content.InstanceAttributes[0].Value, Is.EqualTo(14));
            Assert.That(reply.Kitbag.Slots[2].Content.Count, Is.EqualTo(5));
            Assert.That(reply.Kitbag.Slots[8].Content.Id, Is.Zero, "an empty grid");
            Assert.That(reply.Shortcuts, Has.Count.EqualTo(36));
            Assert.That(reply.Shortcuts[0].Type, Is.EqualTo(2));
            Assert.That(reply.Shortcuts[0].GridId, Is.EqualTo(475));
            Assert.That(reply.Boats, Is.Empty);
            Assert.That(reply.MainCharacterId, Is.EqualTo(1));
        }

        [Test]
        public void A_pinned_reply_ends_on_its_last_byte()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedReplyHex));

            EnterMapReply.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_pinned_reply_writes_back_the_bytes_the_server_sent()
        {
            var packet = new PacketWriter();
            Read(PinnedReplyHex).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(PinnedReplyHex)));
        }

        [Test]
        public void A_refused_reply_carries_its_code_and_stops_reading()
        {
            var packet = new PacketReader(Hex.Bytes("0008" + "FFFF"));

            var reply = EnterMapReply.Read(packet);

            Assert.That(reply.IsRefused, Is.True);
            Assert.That(reply.Code, Is.EqualTo(EnterFailed));
            Assert.That(packet.Remaining, Is.EqualTo(2), "nothing past the code is read");
        }

        [Test]
        public void A_refused_reply_writes_its_code_only()
        {
            var packet = new PacketWriter();
            EnterMapReply.Refused(EnterFailed).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes("0008")));
        }

        [Test]
        public void A_reply_with_a_boat_still_ends_on_its_last_byte()
        {
            var pinned = Read(PinnedReplyHex);
            var boat = new Boat(pinned.Actor, pinned.AttributeType, pinned.Attributes, pinned.Kitbag,
                pinned.ServerTime, pinned.SkillStates);
            var packet = new PacketReader(WriteToBytes(Rebuild(pinned, pinned.Kitbag, new[] { boat })));

            var read = EnterMapReply.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(read.Boats, Has.Count.EqualTo(1));
            Assert.That(read.Boats[0].Actor.Name, Is.EqualTo("TestPirate"));
            Assert.That(read.Boats[0].Attributes, Has.Count.EqualTo(74));
            Assert.That(read.Boats[0].Kitbag.Slots, Has.Count.EqualTo(24));
            Assert.That(read.MainCharacterId, Is.EqualTo(1));
        }

        [Test]
        public void A_vessel_deed_in_the_kitbag_carries_the_world_id_of_its_boat()
        {
            var pinned = Read(PinnedReplyHex);
            var packet = new PacketReader(WriteToBytes(Rebuild(pinned, AddVesselDeed(pinned.Kitbag), pinned.Boats)));

            var read = EnterMapReply.Read(packet);

            Assert.That(packet.Remaining, Is.Zero);
            Assert.That(read.Kitbag.Slots[23].Content.BoatWorldId, Is.EqualTo(77));
            Assert.That(read.Kitbag.Slots[23].Content.InstanceId, Is.EqualTo(5));
        }

        [Test]
        public void A_reply_that_runs_out_of_bytes_throws()
        {
            var pinned = Hex.Bytes(PinnedReplyHex);
            var truncated = new byte[pinned.Length - 1];
            Array.Copy(pinned, truncated, truncated.Length);

            Assert.That(
                () => EnterMapReply.Read(new PacketReader(truncated)),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_refusal_is_not_built_from_the_code_of_an_accepted_reply()
        {
            Assert.That(() => EnterMapReply.Refused(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        private static EnterMapReply Read(string hex)
        {
            return EnterMapReply.Read(new PacketReader(Hex.Bytes(hex)));
        }

        private static byte[] WriteToBytes(EnterMapReply reply)
        {
            var packet = new PacketWriter();
            reply.Write(packet);

            return packet.ToArray();
        }

        private static long FindValue(IReadOnlyList<CharacterAttribute> attributes, byte id)
        {
            return attributes.Single(attribute => attribute.Id == id).Value;
        }

        private static KitbagSync AddVesselDeed(KitbagSync kitbag)
        {
            var deed = new ItemGrid(
                VesselDeed, 0, 0, 1, new short[] { 0, 0 }, new short[] { 0, 0 }, 0, true, true, 0, 77, 0, 5, null);

            return new KitbagSync(kitbag.Type, kitbag.Capacity,
                kitbag.Slots.Take(23).Append(new KitbagSlot(23, deed)).ToArray());
        }

        private static EnterMapReply Rebuild(EnterMapReply reply, KitbagSync kitbag, IReadOnlyList<Boat> boats)
        {
            return new EnterMapReply(reply.SwitchMap, reply.IsAutoLocked, reply.IsKitbagLocked, reply.Imps,
                reply.Actor, reply.DefaultSkillId, reply.SkillBagType, reply.SkillBag, reply.ServerTime,
                reply.SkillStates, reply.AttributeType, reply.Attributes, kitbag, reply.Shortcuts, boats,
                reply.MainCharacterId);
        }
    }
}
