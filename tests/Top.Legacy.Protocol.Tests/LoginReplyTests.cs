using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Login;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class LoginReplyTests
    {
        private const string Accepted = "0000";
        private const string SlotCapOfFive = "05";
        private const string ThreeRows = "03";
        private const string EmptyRow = "00";
        private const string SecondPasswordSet = "01";

        private const string Equipped =
            "03E803E903EA03EB03EC03ED03EE03EF03F003F103F203F303F403F503F603F7" +
            "03F803F903FA03FB03FC03FD03FE03FF04000401040204030404040504060407" +
            "04080409";

        private const string Unequipped =
            "00000000000000000000000000000000000000000000000000000000000000000000" +
            "00000000000000000000000000000000000000000000000000000000000000000000";

        private const string BlackbeardRow =
            "01" + "000B426C61636B626561726400" + "00094578706C6F72657200" + "002A" + "0003" + Equipped;

        private const string AnneRow =
            "01" + "0005416E6E6500" + "000A48657262616C69737400" + "0007" + "0002" + Unequipped;

        private const string PinnedReplyHex =
            Accepted + SlotCapOfFive + ThreeRows + BlackbeardRow + EmptyRow + AnneRow + SecondPasswordSet;

        private const string RefusedReplyHex =
            "03EA" + "0013" + "50617373776F726420696E636F7272656374" + "00";

        private const ushort WrongPassword = 1002;
        private const ushort VersionRefused = 7;
        private const string GateText = "Password incorrect";

        [Test]
        public void A_pinned_reply_decodes_its_slot_cap_its_characters_and_its_second_password_flag()
        {
            var reply = Read(PinnedReplyHex);

            Assert.That(reply.IsRefused, Is.False);
            Assert.That(reply.SlotCap, Is.EqualTo(5));
            Assert.That(reply.HasSecondPassword, Is.True);
            Assert.That(Names(reply), Is.EqualTo(new[] { "Blackbeard", "Anne" }));
            Assert.That(reply.Characters[0].Job, Is.EqualTo("Explorer"));
            Assert.That(reply.Characters[0].Level, Is.EqualTo(42));
            Assert.That(reply.Characters[0].TypeId, Is.EqualTo(3));
            Assert.That(reply.Characters[1].Job, Is.EqualTo("Herbalist"));
            Assert.That(reply.Characters[1].Level, Is.EqualTo(7));
            Assert.That(reply.Characters[1].TypeId, Is.EqualTo(2));
        }

        [Test]
        public void Every_equipment_slot_decodes_in_the_order_it_arrives()
        {
            Assert.That(Read(PinnedReplyHex).Characters[0].EquipmentIds, Is.EqualTo(CountingEquipment()));
        }

        [Test]
        public void A_row_the_gate_could_not_read_is_counted_and_skipped()
        {
            Assert.That(Read(PinnedReplyHex).Characters, Has.Count.EqualTo(2), "three rows, one of them empty");
        }

        [Test]
        public void An_account_with_no_characters_decodes_to_an_empty_list()
        {
            var reply = Read(Accepted + SlotCapOfFive + "00" + "00");

            Assert.That(reply.IsRefused, Is.False);
            Assert.That(reply.SlotCap, Is.EqualTo(5));
            Assert.That(reply.Characters, Is.Empty);
            Assert.That(reply.HasSecondPassword, Is.False);
        }

        [Test]
        public void A_refused_login_decodes_its_code_and_the_text_the_gate_sent()
        {
            var reply = Read(RefusedReplyHex);

            Assert.That(reply.IsRefused, Is.True);
            Assert.That(reply.Code, Is.EqualTo(WrongPassword));
            Assert.That(reply.RefusalText, Is.EqualTo(GateText));
            Assert.That(reply.Characters, Is.Empty);
        }

        [Test]
        public void A_refusal_that_repeats_the_code_in_place_of_a_text_decodes_with_no_text()
        {
            var reply = Read("03EA" + "03EA");

            Assert.That(
                reply.Code,
                Is.EqualTo(WrongPassword),
                "the reference gate appends the code to what the group server answered");
            Assert.That(reply.RefusalText, Is.Empty);
        }

        [Test]
        public void A_refusal_whose_leftovers_are_longer_than_a_code_decodes_with_no_text()
        {
            var reply = Read("03EA" + "0000002A" + "03EA");

            Assert.That(reply.Code, Is.EqualTo(WrongPassword));
            Assert.That(reply.RefusalText, Is.Empty, "what the account server left behind is not a string");
        }

        [Test]
        public void A_refusal_with_nothing_after_the_code_decodes_with_no_text()
        {
            var reply = Read("0007");

            Assert.That(reply.Code, Is.EqualTo(VersionRefused));
            Assert.That(reply.RefusalText, Is.Empty);
        }

        [Test]
        public void A_refused_reply_round_trips()
        {
            var read = RoundTrip(LoginReply.Refused(WrongPassword, GateText));

            Assert.That(read.Code, Is.EqualTo(WrongPassword));
            Assert.That(read.RefusalText, Is.EqualTo(GateText));
        }

        [Test]
        public void A_refused_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            LoginReply.Refused(WrongPassword, GateText).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(Hex.Bytes(RefusedReplyHex)));
        }

        [Test]
        public void An_accepted_reply_round_trips()
        {
            var read = RoundTrip(LoginReply.Accepted(5, new[] { Blackbeard(), Anne() }, true));

            Assert.That(read.SlotCap, Is.EqualTo(5));
            Assert.That(read.HasSecondPassword, Is.True);
            Assert.That(Names(read), Is.EqualTo(new[] { "Blackbeard", "Anne" }));
            Assert.That(read.Characters[0].Job, Is.EqualTo("Explorer"));
            Assert.That(read.Characters[0].Level, Is.EqualTo(42));
            Assert.That(read.Characters[0].TypeId, Is.EqualTo(3));
            Assert.That(read.Characters[0].EquipmentIds, Is.EqualTo(CountingEquipment()));
        }

        [Test]
        public void An_empty_row_is_dropped_on_the_way_back_out()
        {
            var packet = new PacketWriter();
            Read(PinnedReplyHex).Write(packet);

            Assert.That(
                packet.ToArray(),
                Is.EqualTo(Hex.Bytes(
                    Accepted + SlotCapOfFive + "02" + BlackbeardRow + AnneRow + SecondPasswordSet)),
                "a decoded reply holds characters, not slots, so the empty row cannot be written back");
        }

        [Test]
        public void An_accepted_reply_writes_the_bytes_the_gate_sends()
        {
            var packet = new PacketWriter();
            LoginReply.Accepted(5, new[] { Blackbeard() }, true).Write(packet);

            Assert.That(
                packet.ToArray(),
                Is.EqualTo(Hex.Bytes(Accepted + SlotCapOfFive + "01" + BlackbeardRow + SecondPasswordSet)));
        }

        [Test]
        public void A_reply_that_runs_out_of_bytes_throws()
        {
            var pinned = Hex.Bytes(PinnedReplyHex);
            var truncated = new byte[pinned.Length - 1];
            Array.Copy(pinned, truncated, truncated.Length);

            Assert.That(
                () => LoginReply.Read(new PacketReader(truncated)),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_reply_whose_rows_never_arrive_throws()
        {
            Assert.That(
                () => Read(Accepted + SlotCapOfFive + ThreeRows),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_refusal_is_not_built_from_the_code_of_an_accepted_login()
        {
            Assert.That(
                () => LoginReply.Refused(0, GateText),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void A_summary_keeps_its_own_copy_of_the_equipment_ids()
        {
            var equipmentIds = CountingEquipment();
            var summary = new CharacterSummary("Anne", "Herbalist", 7, 2, equipmentIds);

            equipmentIds[0] = 0;

            Assert.That(summary.EquipmentIds[0], Is.EqualTo(1000));
        }

        [Test]
        public void A_summary_takes_one_id_per_equipment_slot()
        {
            Assert.That(
                () => new CharacterSummary("Anne", "Herbalist", 7, 2, new ushort[] { 1, 2, 3 }),
                Throws.InstanceOf<ArgumentException>());
        }

        private static LoginReply Read(string hex)
        {
            return LoginReply.Read(new PacketReader(Hex.Bytes(hex)));
        }

        private static LoginReply RoundTrip(LoginReply reply)
        {
            var packet = new PacketWriter();
            reply.Write(packet);

            return LoginReply.Read(new PacketReader(packet.ToArray()));
        }

        private static string[] Names(LoginReply reply)
        {
            return reply.Characters.Select(character => character.Name).ToArray();
        }

        private static ushort[] CountingEquipment()
        {
            return Enumerable.Range(1000, CharacterSummary.EquipmentSlots).Select(id => (ushort)id).ToArray();
        }

        private static CharacterSummary Blackbeard()
        {
            return new CharacterSummary("Blackbeard", "Explorer", 42, 3, CountingEquipment());
        }

        private static CharacterSummary Anne()
        {
            return new CharacterSummary("Anne", "Herbalist", 7, 2, new ushort[CharacterSummary.EquipmentSlots]);
        }
    }
}
