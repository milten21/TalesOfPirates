using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class KitbagUnlockRequest
    {
        public KitbagUnlockRequest(string code)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        public string Code { get; }

        public static KitbagUnlockRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new KitbagUnlockRequest(packet.ReadString());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteString(Code);
        }
    }
}
