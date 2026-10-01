using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class ActionNoticeTests
    {
        private const string Header = "00000004" + "0000002A";

        private const string LookAction = "05";

        private const string MoveAction = "01";

        private const string SkillAction = "02";

        private const string KitbagAction = "06";

        private const string TemporaryKitbagAction = "1C";

        private const string PinnedKitbagLockedHex = "00000002" + "00000001" + "0F" + "0007";

        private const string KitbagSwitch = "05" + "0000" + "0121" + "00000BEE" + "0000" + "0001" + "2710" + "2710" +
            "0000" + "0000" + "00" + "01" + "01" + "00000000" + "00000000" + "00000000" + "00" + "0005" + "0000" + "FFFF";

        private const string CapturedHeader = "00000004" + "00000001";

        private const string MovingBody = "0000" + "0010" + "BD5C0300" + "413D0400" + "3D5B0300" + "413D0400";

        private const string ArrivedBody = "0001" + "0001" + "0010" + "3D5B0300" + "413D0400" + "C95A0300" + "413D0400";

        private const string BlockedBody = "0002" + "0001" + "0010" + "BD5C0300" + "413D0400" + "BD5C0300" + "413D0400";

        private const string CancelledBody = "0004" + "0001" + "0010" + "6E5C0300" + "2B3C0400" + "AA5C0300" + "2B3C0400";

        private const string EmptyLink = "0000" + "00000000" + "0000";

        private const string ShortSwordInRightHand =
            "0001" + "0000BEEF" + "0000" + "0032" + "0000" + "01" + "01" + "00000000";

        private static readonly string LookChange =
            "01" + "0001" + "00" + "07D0" +
            string.Concat(Enumerable.Repeat(EmptyLink, 9)) + ShortSwordInRightHand +
            string.Concat(Enumerable.Repeat(EmptyLink, 24));

        [Test]
        public void A_look_action_names_the_character_and_carries_its_look()
        {
            var packet = new PacketReader(Hex.Bytes(Header + LookAction + LookChange));

            var notice = ActionNotice.Read(packet);

            Assert.That(notice.WorldId, Is.EqualTo(4));
            Assert.That(notice.PacketId, Is.EqualTo(42));
            Assert.That(notice.ActionType, Is.EqualTo(LookNotice.Action));
            var look = ((LookNotice)notice).Look;
            Assert.That(look.HairId, Is.EqualTo(2000));
            Assert.That(look.Links[9].Id, Is.EqualTo(1), "the right hand holds the short sword");
            Assert.That(look.Links.Count(link => !link.IsEmpty), Is.EqualTo(1));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_look_action_writes_back_the_bytes_it_was_read_from()
        {
            var bytes = Hex.Bytes(Header + LookAction + LookChange);
            var packet = new PacketWriter();

            ActionNotice.Read(new PacketReader(bytes)).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_move_action_that_goes_on_carries_the_points_travelled_and_no_stop_state()
        {
            var packet = new PacketReader(Hex.Bytes(CapturedHeader + MoveAction + MovingBody));

            var notice = ActionNotice.Read(packet);

            Assert.That(notice.WorldId, Is.EqualTo(4));
            Assert.That(notice.PacketId, Is.EqualTo(1));
            Assert.That(notice.ActionType, Is.EqualTo(MoveNotice.Action));
            var move = ((MoveNotice)notice).Move;
            Assert.That(move.State, Is.Zero);
            Assert.That(move.StopState, Is.Null);
            Assert.That(move.Points, Is.EqualTo(new[] { new Point(220349, 277825), new Point(219965, 277825) }));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_move_action_that_ends_carries_the_state_it_ends_with_and_the_state_to_stop_in()
        {
            var packet = new PacketReader(Hex.Bytes(CapturedHeader + MoveAction + ArrivedBody));

            var move = ((MoveNotice)ActionNotice.Read(packet)).Move;

            Assert.That(move.State, Is.EqualTo(1), "arrived");
            Assert.That(move.StopState, Is.EqualTo(1));
            Assert.That(move.Points, Is.EqualTo(new[] { new Point(219965, 277825), new Point(219849, 277825) }));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_move_action_that_hits_an_obstacle_ends_where_it_started_its_step()
        {
            var notice = ActionNotice.Read(new PacketReader(Hex.Bytes(CapturedHeader + MoveAction + BlockedBody)));

            var move = ((MoveNotice)notice).Move;
            Assert.That(move.State, Is.EqualTo(2), "blocked");
            Assert.That(move.StopState, Is.EqualTo(1));
            Assert.That(move.Points, Is.EqualTo(new[] { new Point(220349, 277825), new Point(220349, 277825) }));
        }

        [Test]
        public void A_move_action_that_an_end_action_stops_ends_cancelled_where_the_step_ended()
        {
            var notice = ActionNotice.Read(new PacketReader(Hex.Bytes(CapturedHeader + MoveAction + CancelledBody)));

            var move = ((MoveNotice)notice).Move;
            Assert.That(move.State, Is.EqualTo(4), "cancelled");
            Assert.That(move.StopState, Is.EqualTo(1));
            Assert.That(move.Points, Is.EqualTo(new[] { new Point(220270, 277547), new Point(220330, 277547) }));
        }

        [TestCase(MovingBody)]
        [TestCase(ArrivedBody)]
        [TestCase(BlockedBody)]
        [TestCase(CancelledBody)]
        public void A_move_action_writes_back_the_bytes_it_was_read_from(string body)
        {
            var bytes = Hex.Bytes(CapturedHeader + MoveAction + body);
            var packet = new PacketWriter();

            ActionNotice.Read(new PacketReader(bytes)).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_move_action_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => ActionNotice.Read(new PacketReader(Hex.Bytes(CapturedHeader + MoveAction + "0000" + "0010" + "BD5C0300"))),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void Another_action_is_read_up_to_its_type_and_carries_no_look_and_no_move()
        {
            var notice = ActionNotice.Read(new PacketReader(Hex.Bytes(Header + SkillAction + "0001")));

            Assert.That(notice.ActionType, Is.EqualTo(2));
            Assert.That(notice, Is.InstanceOf<UnreadActionNotice>());
        }

        [Test]
        public void A_kitbag_action_carries_the_changed_slots_of_the_main_bag()
        {
            var packet = new PacketReader(Hex.Bytes(Header + KitbagAction + KitbagSwitch));

            var notice = ActionNotice.Read(packet);

            Assert.That(notice.ActionType, Is.EqualTo(KitbagNotice.Action));
            var kitbag = ((KitbagNotice)notice).Kitbag;
            Assert.That(kitbag.Slots.Select(slot => slot.GridId), Is.EqualTo(new short[] { 0, 5 }));
            Assert.That(kitbag.Slots[0].Content.Id, Is.EqualTo(289));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_temporary_kitbag_action_carries_the_changed_slots_of_the_temporary_bag()
        {
            var packet = new PacketReader(Hex.Bytes(Header + TemporaryKitbagAction + KitbagSwitch));

            var notice = ActionNotice.Read(packet);

            Assert.That(notice.ActionType, Is.EqualTo(TemporaryKitbagNotice.Action));
            Assert.That(((TemporaryKitbagNotice)notice).Kitbag.Slots, Has.Count.EqualTo(2));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [TestCase(KitbagAction)]
        [TestCase(TemporaryKitbagAction)]
        public void A_kitbag_action_writes_back_the_bytes_it_was_read_from(string action)
        {
            var bytes = Hex.Bytes(Header + action + KitbagSwitch);
            var packet = new PacketWriter();

            ActionNotice.Read(new PacketReader(bytes)).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_move_action_without_a_move_is_refused()
        {
            Assert.That(() => new MoveNotice(4, 42, null), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void An_item_failed_action_the_server_sent_for_a_move_in_a_locked_bag_carries_the_result()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedKitbagLockedHex));

            var notice = ActionNotice.Read(packet);

            Assert.That(notice.WorldId, Is.EqualTo(2));
            Assert.That(notice.PacketId, Is.EqualTo(1));
            Assert.That(notice.ActionType, Is.EqualTo(ItemFailedNotice.Action));
            Assert.That(((ItemFailedNotice)notice).Result, Is.EqualTo(7), "the kitbag is locked");
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_failed_action_writes_back_the_bytes_it_was_read_from()
        {
            var bytes = Hex.Bytes(PinnedKitbagLockedHex);
            var packet = new PacketWriter();

            ActionNotice.Read(new PacketReader(bytes)).Write(packet);

            Assert.That(packet.ToArray(), Is.EqualTo(bytes));
        }

        [Test]
        public void A_look_action_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => ActionNotice.Read(new PacketReader(Hex.Bytes(Header + LookAction + "01000100"))),
                Throws.InstanceOf<InvalidDataException>());
        }
    }
}
