using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class ItemGrid
    {
        private const int EndureCount = 2;
        private const int EnergyCount = 2;

        private static readonly short[] NoWear = new short[EndureCount];

        public static readonly ItemGrid Empty =
            new ItemGrid(0, 0, 0, 0, NoWear, NoWear, 0, false, false, 0, null, null, null, null);

        public ItemGrid(short id, uint dbId, short needLevel, short count, IReadOnlyList<short> endure,
            IReadOnlyList<short> energy, byte forgeLevel, bool isValid, bool isTradable, int expiration,
            uint? boatWorldId, int? forge, int? instanceId, IReadOnlyList<ItemInstanceAttribute> instanceAttributes)
        {
            if (endure == null)
            {
                throw new ArgumentNullException(nameof(endure));
            }

            if (energy == null)
            {
                throw new ArgumentNullException(nameof(energy));
            }

            if (endure.Count != EndureCount || energy.Count != EnergyCount)
            {
                throw new ArgumentException(
                    $"An item carries a current and a maximum endure and energy, not {endure.Count} and {energy.Count}.");
            }

            if (forge.HasValue != instanceId.HasValue)
            {
                throw new ArgumentException("The forge and the instance id arrive together or not at all.");
            }

            if (instanceAttributes != null && !forge.HasValue)
            {
                throw new ArgumentException("Instance attributes follow the forge and the instance id.");
            }

            Id = id;
            DbId = dbId;
            NeedLevel = needLevel;
            Count = count;
            Endure = new[] { endure[0], endure[1] };
            Energy = new[] { energy[0], energy[1] };
            ForgeLevel = forgeLevel;
            IsValid = isValid;
            IsTradable = isTradable;
            Expiration = expiration;
            BoatWorldId = boatWorldId;
            Forge = forge;
            InstanceId = instanceId;
            InstanceAttributes = instanceAttributes;
        }

        public short Id { get; }

        public uint DbId { get; }

        public short NeedLevel { get; }

        public short Count { get; }

        public IReadOnlyList<short> Endure { get; }

        public IReadOnlyList<short> Energy { get; }

        public byte ForgeLevel { get; }

        public bool IsValid { get; }

        public bool IsTradable { get; }

        public int Expiration { get; }

        public uint? BoatWorldId { get; }

        public int? Forge { get; }

        public int? InstanceId { get; }

        public IReadOnlyList<ItemInstanceAttribute> InstanceAttributes { get; }

        public bool IsEmpty => Id == 0;

        public int GetInstanceAttribute(short attributeId)
        {
            foreach (var attribute in InstanceAttributes ?? Array.Empty<ItemInstanceAttribute>())
            {
                if (attribute.Id == attributeId)
                {
                    return attribute.Value;
                }
            }

            return 0;
        }

        public static ItemGrid ReadLink(PacketReader packet, byte syncType)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var id = packet.ReadShort();
            var dbId = packet.ReadUInt();
            var needLevel = packet.ReadShort();

            if (id == 0)
            {
                return new ItemGrid(id, dbId, needLevel, 0, NoWear, NoWear, 0, false, false, 0, null, null, null, null);
            }

            if (syncType == LookInfo.ItemDataSync)
            {
                var endure = new[] { packet.ReadShort(), (short)0 };
                var energy = new[] { packet.ReadShort(), (short)0 };
                var isValid = packet.ReadByte() != 0;
                var isTradable = packet.ReadByte() != 0;

                return new ItemGrid(id, dbId, needLevel, 0, endure, energy, 0, isValid, isTradable,
                    packet.ReadInt(), null, null, null, null);
            }

            var state = ReadState(packet);

            if (packet.ReadByte() == 0)
            {
                return new ItemGrid(id, dbId, needLevel, state.Count, state.Endure, state.Energy, state.ForgeLevel,
                    state.IsValid, state.IsTradable, state.Expiration, null, null, null, null);
            }

            var forge = packet.ReadInt();
            var instanceId = packet.ReadInt();

            return new ItemGrid(id, dbId, needLevel, state.Count, state.Endure, state.Energy, state.ForgeLevel,
                state.IsValid, state.IsTradable, state.Expiration, null, forge, instanceId,
                ReadInstanceAttributes(packet));
        }

        public void WriteLink(PacketWriter packet, byte syncType)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteShort(Id);
            packet.WriteUInt(DbId);
            packet.WriteShort(NeedLevel);

            if (IsEmpty)
            {
                return;
            }

            if (syncType == LookInfo.ItemDataSync)
            {
                packet.WriteShort(Endure[0]);
                packet.WriteShort(Energy[0]);
                packet.WriteByte(IsValid ? (byte)1 : (byte)0);
                packet.WriteByte(IsTradable ? (byte)1 : (byte)0);
                packet.WriteInt(Expiration);

                return;
            }

            WriteState(packet);

            if (!Forge.HasValue)
            {
                packet.WriteByte(0);

                return;
            }

            packet.WriteByte(1);
            packet.WriteInt(Forge.Value);
            packet.WriteInt(InstanceId.Value);
            WriteInstanceAttributes(packet);
        }

        public static ItemGrid ReadKitbagContent(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (boatItemIds == null)
            {
                throw new ArgumentNullException(nameof(boatItemIds));
            }

            var id = packet.ReadShort();

            if (id <= 0)
            {
                return Empty;
            }

            var dbId = packet.ReadUInt();
            var needLevel = packet.ReadShort();
            var state = ReadState(packet);
            uint? boatWorldId = null;

            if (IsBoat(id, boatItemIds))
            {
                boatWorldId = packet.ReadUInt();
            }

            var forge = packet.ReadInt();
            var instanceId = packet.ReadInt();

            return new ItemGrid(id, dbId, needLevel, state.Count, state.Endure, state.Energy, state.ForgeLevel,
                state.IsValid, state.IsTradable, state.Expiration, boatWorldId, forge, instanceId,
                ReadInstanceAttributes(packet));
        }

        public void WriteKitbagContent(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteShort(Id);

            if (IsEmpty)
            {
                return;
            }

            if (!Forge.HasValue)
            {
                throw new InvalidDataException($"Item {Id} in a kitbag carries a forge and an instance id.");
            }

            packet.WriteUInt(DbId);
            packet.WriteShort(NeedLevel);
            WriteState(packet);

            if (BoatWorldId.HasValue)
            {
                packet.WriteUInt(BoatWorldId.Value);
            }

            packet.WriteInt(Forge.Value);
            packet.WriteInt(InstanceId.Value);
            WriteInstanceAttributes(packet);
        }

        private static bool IsBoat(short id, IReadOnlyCollection<short> boatItemIds)
        {
            foreach (var boatItemId in boatItemIds)
            {
                if (boatItemId == id)
                {
                    return true;
                }
            }

            return false;
        }

        private static State ReadState(PacketReader packet)
        {
            var count = packet.ReadShort();
            var endure = new[] { packet.ReadShort(), packet.ReadShort() };
            var energy = new[] { packet.ReadShort(), packet.ReadShort() };
            var forgeLevel = packet.ReadByte();
            var isValid = packet.ReadByte() != 0;
            var isTradable = packet.ReadByte() != 0;

            return new State(count, endure, energy, forgeLevel, isValid, isTradable, packet.ReadInt());
        }

        private void WriteState(PacketWriter packet)
        {
            packet.WriteShort(Count);
            packet.WriteShort(Endure[0]);
            packet.WriteShort(Endure[1]);
            packet.WriteShort(Energy[0]);
            packet.WriteShort(Energy[1]);
            packet.WriteByte(ForgeLevel);
            packet.WriteByte(IsValid ? (byte)1 : (byte)0);
            packet.WriteByte(IsTradable ? (byte)1 : (byte)0);
            packet.WriteInt(Expiration);
        }

        private static IReadOnlyList<ItemInstanceAttribute> ReadInstanceAttributes(PacketReader packet)
        {
            return packet.ReadByte() == 0 ? null : ItemInstanceAttribute.ReadList(packet);
        }

        private void WriteInstanceAttributes(PacketWriter packet)
        {
            if (InstanceAttributes == null)
            {
                packet.WriteByte(0);

                return;
            }

            packet.WriteByte(1);
            ItemInstanceAttribute.WriteList(packet, InstanceAttributes);
        }

        private readonly struct State
        {
            public State(short count, short[] endure, short[] energy, byte forgeLevel, bool isValid,
                bool isTradable, int expiration)
            {
                Count = count;
                Endure = endure;
                Energy = energy;
                ForgeLevel = forgeLevel;
                IsValid = isValid;
                IsTradable = isTradable;
                Expiration = expiration;
            }

            public short Count { get; }

            public short[] Endure { get; }

            public short[] Energy { get; }

            public byte ForgeLevel { get; }

            public bool IsValid { get; }

            public bool IsTradable { get; }

            public int Expiration { get; }
        }
    }
}
