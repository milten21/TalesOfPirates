using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class BoatLook
    {
        public BoatLook(short poseId, short boatId, short header, short body, short engine, short cannon,
            short equipment)
        {
            PoseId = poseId;
            BoatId = boatId;
            Header = header;
            Body = body;
            Engine = engine;
            Cannon = cannon;
            Equipment = equipment;
        }

        public short PoseId { get; }

        public short BoatId { get; }

        public short Header { get; }

        public short Body { get; }

        public short Engine { get; }

        public short Cannon { get; }

        public short Equipment { get; }

        public static BoatLook Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var poseId = packet.ReadShort();
            var boatId = packet.ReadShort();
            var header = packet.ReadShort();
            var body = packet.ReadShort();
            var engine = packet.ReadShort();
            var cannon = packet.ReadShort();

            return new BoatLook(poseId, boatId, header, body, engine, cannon, packet.ReadShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteShort(PoseId);
            packet.WriteShort(BoatId);
            packet.WriteShort(Header);
            packet.WriteShort(Body);
            packet.WriteShort(Engine);
            packet.WriteShort(Cannon);
            packet.WriteShort(Equipment);
        }
    }
}
