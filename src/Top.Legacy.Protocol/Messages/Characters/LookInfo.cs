using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Messages.Account;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class LookInfo
    {
        public const int LinkCount = CharacterSummary.EquipmentSlots;

        public const byte FullSync = 0;
        public const byte ItemDataSync = 1;

        public const short TotemMarker = 9998;
        public const short BothHandsMarker = 9999;

        private static readonly ItemGrid[] NoLinks = Array.Empty<ItemGrid>();

        private LookInfo(byte syncType, short typeId, BoatLook boat, short hairId, IReadOnlyList<ItemGrid> links)
        {
            SyncType = syncType;
            TypeId = typeId;
            Boat = boat;
            HairId = hairId;
            Links = links;
        }

        public byte SyncType { get; }

        public short TypeId { get; }

        public BoatLook Boat { get; }

        public short HairId { get; }

        public IReadOnlyList<ItemGrid> Links { get; }

        public bool IsBoat => Boat != null;

        public bool IsItemDataSync => SyncType == ItemDataSync;

        public static LookInfo OfBoat(byte syncType, short typeId, BoatLook boat)
        {
            if (boat == null)
            {
                throw new ArgumentNullException(nameof(boat));
            }

            return new LookInfo(syncType, typeId, boat, 0, NoLinks);
        }

        public static LookInfo OfCharacter(byte syncType, short typeId, short hairId, IReadOnlyList<ItemGrid> links)
        {
            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            if (links.Count != LinkCount)
            {
                throw new ArgumentException(
                    $"A look carries {LinkCount} equipment links and {links.Count} were given.", nameof(links));
            }

            var copy = new ItemGrid[LinkCount];

            for (var link = 0; link < copy.Length; link++)
            {
                copy[link] = links[link] ?? throw new ArgumentNullException(nameof(links));
            }

            return new LookInfo(syncType, typeId, null, hairId, copy);
        }

        public static LookInfo Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var syncType = packet.ReadByte();
            var typeId = packet.ReadShort();

            if (packet.ReadByte() == 1)
            {
                return OfBoat(syncType, typeId, BoatLook.Read(packet));
            }

            var hairId = packet.ReadShort();
            var links = new ItemGrid[LinkCount];

            for (var link = 0; link < links.Length; link++)
            {
                links[link] = ItemGrid.ReadLink(packet, syncType);
            }

            return OfCharacter(syncType, typeId, hairId, links);
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(SyncType);
            packet.WriteShort(TypeId);

            if (IsBoat)
            {
                packet.WriteByte(1);
                Boat.Write(packet);

                return;
            }

            packet.WriteByte(0);
            packet.WriteShort(HairId);

            foreach (var link in Links)
            {
                link.WriteLink(packet, SyncType);
            }
        }
    }
}
