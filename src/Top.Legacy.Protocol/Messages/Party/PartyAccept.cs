using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Party
{
    public class PartyAccept
    {
        public PartyAccept(uint inviterId)
        {
            InviterId = inviterId;
        }

        public uint InviterId { get; }

        public static PartyAccept Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            return new PartyAccept(packet.ReadUInt());
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
