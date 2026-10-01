using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class AppendLook
    {
        public const int Slots = 4;

        public AppendLook(short lookId, bool isValid)
        {
            LookId = lookId;
            IsValid = isValid;
        }

        public short LookId { get; }

        public bool IsValid { get; }

        public static IReadOnlyList<AppendLook> ReadList(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var looks = new AppendLook[Slots];

            for (var slot = 0; slot < looks.Length; slot++)
            {
                var lookId = packet.ReadShort();
                looks[slot] = new AppendLook(lookId, lookId != 0 && packet.ReadByte() != 0);
            }

            return looks;
        }

        public static void WriteList(PacketWriter packet, IReadOnlyList<AppendLook> looks)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (looks == null)
            {
                throw new ArgumentNullException(nameof(looks));
            }

            if (looks.Count != Slots)
            {
                throw new ArgumentException(
                    $"A character carries {Slots} append looks and {looks.Count} were given.", nameof(looks));
            }

            foreach (var look in looks)
            {
                packet.WriteShort(look.LookId);

                if (look.LookId != 0)
                {
                    packet.WriteByte(look.IsValid ? (byte)1 : (byte)0);
                }
            }
        }
    }
}
