using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class TemporaryKitbagSyncRequest
    {
        public static TemporaryKitbagSyncRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new TemporaryKitbagSyncRequest();
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }
        }
    }
}
