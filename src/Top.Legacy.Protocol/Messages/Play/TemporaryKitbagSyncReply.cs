using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class TemporaryKitbagSyncReply
    {
        public TemporaryKitbagSyncReply(KitbagSync kitbag)
        {
            Kitbag = kitbag ?? throw new ArgumentNullException(nameof(kitbag));
        }

        public KitbagSync Kitbag { get; }

        public static TemporaryKitbagSyncReply Read(PacketReader packet)
        {
            return Read(packet, KitbagSlot.DefaultBoatItemIds);
        }

        public static TemporaryKitbagSyncReply Read(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            return new TemporaryKitbagSyncReply(KitbagSync.Read(packet, boatItemIds));
        }

        public void Write(PacketWriter packet)
        {
            Kitbag.Write(packet);
        }
    }
}
