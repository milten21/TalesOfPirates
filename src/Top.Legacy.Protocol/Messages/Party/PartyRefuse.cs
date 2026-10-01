using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyRefuse
    {
        public PartyRefuse(uint inviterId)
        {
            InviterId = inviterId;
        }

        public uint InviterId { get; }

        public static PartyRefuse Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new PartyRefuse(packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(InviterId);
        }
    }
}
