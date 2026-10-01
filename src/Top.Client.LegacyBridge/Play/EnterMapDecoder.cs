using System;
using Top.Client.LegacyBridge.Inventory;
using Top.Client.Messages;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Legacy.Protocol.Messages.Play;
using Top.Legacy.Protocol.Packets;

namespace Top.Client.LegacyBridge.Play
{
    public static class EnterMapDecoder
    {
        public static IServerEvent Decode(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var reply = EnterMapReply.Read(packet);

            if (reply.IsRefused)
            {
                return BeginPlayFailedEventMapper.ToBeginPlayFailedEvent(reply.Code);
            }

            return new MapEnteredEvent(reply.SwitchMap.MapName, (EnterType)reply.SwitchMap.EnterType,
                CharacterStateMapper.ToCharacterState(reply.Actor, reply.Attributes),
                BagSyncMapper.ToBagSync(reply.Kitbag), reply.IsKitbagLocked,
                EquipmentMapper.ToEquipment(reply.Actor.Look));
        }
    }
}
