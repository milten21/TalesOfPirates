using System;
using System.Linq;
using Top.Client.LegacyBridge.Inventory;
using Top.Client.Messages;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class ActionNoticeDecoder
    {
        public static IServerEvent? Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return ActionNotice.Read(packet) switch
            {
                MoveNotice notice => DecodeMove(notice.WorldId, notice.Move),
                LookNotice notice => DecodeLook(notice.WorldId, notice.Look),
                KitbagNotice notice => new BagChangedEvent(notice.WorldId, BagSyncMapper.ToBagSync(notice.Kitbag)),
                TemporaryKitbagNotice notice => new TemporaryBagChangedEvent(BagSyncMapper.ToBagSync(notice.Kitbag)),
                ItemFailedNotice notice => new ItemActionFailedEvent(ItemActionFailureMapper.ToFailure(notice.Result)),
                _ => null,
            };
        }

        private static IServerEvent? DecodeLook(uint worldId, LookInfo look)
        {
            if (CharacterStateMapper.ToShipLook(look) is { } shipLook)
            {
                return new ShipLookChangedEvent(worldId, shipLook);
            }

            if (look.IsItemDataSync)
            {
                return FairyMapper.ToFairyConditionChangedEvent(worldId, look);
            }

            return CharacterStateMapper.ToCharacterLook(look) is { } characterLook
                ? new CharacterLookChangedEvent(worldId, characterLook, EquipmentMapper.ToEquipment(look))
                : null;
        }

        private static IServerEvent? DecodeMove(uint worldId, MoveInfo move)
        {
            if (move.Points.Count == 0)
            {
                return null;
            }

            return new CharacterMovedEvent(
                worldId,
                move.Points.Select(MapPointMapper.ToMapPoint).ToArray(),
                (MoveStates)move.State,
                move.StopState);
        }
    }
}
