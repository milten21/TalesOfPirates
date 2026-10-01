using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class ChangeSecondPasswordRequest
    {
        public ChangeSecondPasswordRequest(string oldCode, string newCode)
        {
            OldCode = oldCode ?? throw new ArgumentNullException(nameof(oldCode));
            NewCode = newCode ?? throw new ArgumentNullException(nameof(newCode));
        }

        public string OldCode { get; }

        public string NewCode { get; }

        public static ChangeSecondPasswordRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var oldCode = packet.ReadString();

            return new ChangeSecondPasswordRequest(oldCode, packet.ReadString());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteString(OldCode);
            packet.WriteString(NewCode);
        }
    }
}
