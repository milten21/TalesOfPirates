using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Account
{
    public class CreateSecondPasswordRequest
    {
        public CreateSecondPasswordRequest(string code)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
        }

        public string Code { get; }

        public static CreateSecondPasswordRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new CreateSecondPasswordRequest(packet.ReadString());
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
