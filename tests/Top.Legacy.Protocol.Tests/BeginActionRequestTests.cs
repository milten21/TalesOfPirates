using System;
using System.IO;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class BeginActionRequestTests
    {
        private const string Header = "00000001" + "00000001";

        private const string MoveAction = "01";

        private const string TwoMeterWalkWest = "0010" + "A36D0300" + "33500400" + "DB6C0300" + "33500400";

        private const string PinnedMoveHex = Header + MoveAction + TwoMeterWalkWest;

        private const string ItemMoveAction = "0C";

        private const string WholeStackFromFirstToSeventhSlot = "0000" + "0014" + "0006";

        private const string ItemMoveHex = Header + ItemMoveAction + WholeStackFromFirstToSeventhSlot;

        private const string TemporaryItemDragAction = "1D";

        private const string WholeStackFromThirdTemporarySlotToEighthSlot = "0002" + "0005" + "0007";

        private const string TemporaryItemDragHex =
            Header + TemporaryItemDragAction + WholeStackFromThirdTemporarySlotToEighthSlot;

        private const string ItemUseAction = "0B";

        private const string ItemUseHex = Header + ItemUseAction + "0005" + "FFFF";

        private const string ItemUnfixAction = "0A";

        private const string ItemUnfixToSlotHex = Header + ItemUnfixAction + "07" + "000C";

        private const string ItemUnfixToGroundHex =
            Header + ItemUnfixAction + "02" + "FFFE" + "00036DA3" + "00045033";

        private const string ItemThrowAction = "09";

        private const string ThreeFromFifthSlotAtTheDropPosition = "0004" + "0003" + "00036DA3" + "00045033";

        private const string ItemThrowHex = Header + ItemThrowAction + ThreeFromFifthSlotAtTheDropPosition;

        private const string ItemDeleteAction = "0D";

        private const string ItemDeleteHex = Header + ItemDeleteAction + "0005";

        private static readonly Point[] PinnedPath = { new Point(224675, 282675), new Point(224475, 282675) };

        [Test]
        public void A_move_writes_the_bytes_the_reference_server_accepted()
        {
            var request = new MoveRequest(1, 1, PinnedPath);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(PinnedMoveHex)));
        }

        [Test]
        public void A_pinned_move_names_the_character_the_packet_and_the_path()
        {
            var packet = new PacketReader(Hex.Bytes(PinnedMoveHex));

            var request = BeginActionRequest.Read(packet);

            Assert.That(request.WorldId, Is.EqualTo(1));
            Assert.That(request.PacketId, Is.EqualTo(1));
            Assert.That(request.ActionType, Is.EqualTo(MoveRequest.Action));
            Assert.That(((MoveRequest)request).Path, Is.EqualTo(PinnedPath));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_move_without_a_path_is_refused()
        {
            Assert.That(
                () => new MoveRequest(1, 1, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void A_move_that_runs_out_of_bytes_throws()
        {
            Assert.That(
                () => BeginActionRequest.Read(new PacketReader(Hex.Bytes(Header + MoveAction + "0010A36D0300"))),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void An_item_move_writes_the_source_slot_the_count_and_the_target_slot()
        {
            var request = new ItemMoveRequest(1, 1, 0, 20, 6);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemMoveHex)));
        }

        [Test]
        public void An_item_move_names_the_source_slot_the_count_and_the_target_slot()
        {
            var packet = new PacketReader(Hex.Bytes(ItemMoveHex));

            var request = (ItemMoveRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(ItemMoveRequest.Action));
            Assert.That(request.SourceGridId, Is.EqualTo(0));
            Assert.That(request.Count, Is.EqualTo(20));
            Assert.That(request.TargetGridId, Is.EqualTo(6));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_move_with_part_of_a_stack_writes_the_part_as_its_count()
        {
            var request = new ItemMoveRequest(1, 1, 3, 7, 4);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(Header + ItemMoveAction + "0003" + "0007" + "0004")));
        }

        [Test]
        public void A_temporary_item_drag_writes_the_temporary_slot_the_count_and_the_bag_slot()
        {
            var request = new TemporaryKitbagDragRequest(1, 1, 2, 5, 7);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(TemporaryItemDragHex)));
        }

        [Test]
        public void A_temporary_item_drag_names_the_temporary_slot_the_count_and_the_bag_slot()
        {
            var packet = new PacketReader(Hex.Bytes(TemporaryItemDragHex));

            var request = (TemporaryKitbagDragRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(TemporaryKitbagDragRequest.Action));
            Assert.That(request.SourceGridId, Is.EqualTo(2));
            Assert.That(request.Count, Is.EqualTo(5));
            Assert.That(request.TargetGridId, Is.EqualTo(7));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void A_temporary_item_drag_to_the_first_free_bag_slot_writes_the_first_free_slot()
        {
            var request = new TemporaryKitbagDragRequest(1, 1, 2, 5, TemporaryKitbagDragRequest.FirstFreeGridId);

            Assert.That(WriteToBytes(request),
                Is.EqualTo(Hex.Bytes(Header + TemporaryItemDragAction + "0002" + "0005" + "FFFF")));
        }

        [Test]
        public void An_item_use_writes_the_bag_slot_and_the_target()
        {
            var request = new ItemUseRequest(1, 1, 5, ItemUseRequest.FirstFreeLink);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemUseHex)));
        }

        [Test]
        public void An_item_use_names_the_bag_slot_and_the_target()
        {
            var packet = new PacketReader(Hex.Bytes(ItemUseHex));

            var request = (ItemUseRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(ItemUseRequest.Action));
            Assert.That(request.GridId, Is.EqualTo(5));
            Assert.That(request.TargetGridId, Is.EqualTo(-1));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_use_into_the_second_link_writes_its_own_target()
        {
            var request = new ItemUseRequest(1, 1, 5, ItemUseRequest.SecondLink);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(Header + ItemUseAction + "0005" + "FFFE")));
        }

        [Test]
        public void An_item_unfix_to_a_bag_slot_writes_the_link_and_the_slot_only()
        {
            var request = new ItemUnfixRequest(1, 1, 7, 12);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemUnfixToSlotHex)));
        }

        [Test]
        public void An_item_unfix_to_a_bag_slot_names_the_link_and_the_slot()
        {
            var packet = new PacketReader(Hex.Bytes(ItemUnfixToSlotHex));

            var request = (ItemUnfixRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(ItemUnfixRequest.Action));
            Assert.That(request.LinkId, Is.EqualTo(7));
            Assert.That(request.GridId, Is.EqualTo(12));
            Assert.That(request.DropPosition, Is.Null);
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_unfix_to_the_ground_writes_the_drop_position_after_the_ground_slot()
        {
            var request = new ItemUnfixRequest(1, 1, 2, new Point(224675, 282675));

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemUnfixToGroundHex)));
        }

        [Test]
        public void An_item_unfix_to_the_ground_names_the_drop_position()
        {
            var packet = new PacketReader(Hex.Bytes(ItemUnfixToGroundHex));

            var request = (ItemUnfixRequest)BeginActionRequest.Read(packet);

            Assert.That(request.LinkId, Is.EqualTo(2));
            Assert.That(request.GridId, Is.EqualTo(ItemUnfixRequest.GroundGridId));
            Assert.That(request.DropPosition, Is.EqualTo(new Point(224675, 282675)));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_unfix_to_the_first_free_bag_slot_writes_no_position()
        {
            var request = new ItemUnfixRequest(1, 1, 0, ItemUnfixRequest.FirstFreeGridId);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(Header + ItemUnfixAction + "00" + "FFFF")));
        }

        [Test]
        public void An_item_unfix_to_the_ground_slot_without_a_position_is_refused()
        {
            Assert.That(
                () => new ItemUnfixRequest(1, 1, 0, ItemUnfixRequest.GroundGridId),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void An_item_throw_writes_the_bag_slot_the_count_and_the_drop_position()
        {
            var request = new ItemThrowRequest(1, 1, 4, 3, new Point(224675, 282675));

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemThrowHex)));
        }

        [Test]
        public void An_item_throw_names_the_bag_slot_the_count_and_the_drop_position()
        {
            var packet = new PacketReader(Hex.Bytes(ItemThrowHex));

            var request = (ItemThrowRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(ItemThrowRequest.Action));
            Assert.That(request.GridId, Is.EqualTo(4));
            Assert.That(request.Count, Is.EqualTo(3));
            Assert.That(request.DropPosition, Is.EqualTo(new Point(224675, 282675)));
            Assert.That(packet.Remaining, Is.Zero);
        }

        [Test]
        public void An_item_delete_writes_the_bag_slot()
        {
            var request = new ItemDeleteRequest(1, 1, 5);

            Assert.That(WriteToBytes(request), Is.EqualTo(Hex.Bytes(ItemDeleteHex)));
        }

        [Test]
        public void An_item_delete_names_the_bag_slot()
        {
            var packet = new PacketReader(Hex.Bytes(ItemDeleteHex));

            var request = (ItemDeleteRequest)BeginActionRequest.Read(packet);

            Assert.That(request.ActionType, Is.EqualTo(ItemDeleteRequest.Action));
            Assert.That(request.GridId, Is.EqualTo(5));
            Assert.That(packet.Remaining, Is.Zero);
        }

        private static byte[] WriteToBytes(BeginActionRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }
    }
}
