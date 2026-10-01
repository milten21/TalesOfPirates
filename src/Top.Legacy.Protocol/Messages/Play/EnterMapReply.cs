using System;
using System.Collections.Generic;
using System.IO;
using Top.Legacy.Protocol.Messages.Characters;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class EnterMapReply
    {
        private static readonly SkillSlot[] NoSkills = Array.Empty<SkillSlot>();
        private static readonly SkillState[] NoSkillStates = Array.Empty<SkillState>();
        private static readonly CharacterAttribute[] NoAttributes = Array.Empty<CharacterAttribute>();
        private static readonly KitbagSync NoKitbag =
            new KitbagSync(KitbagSync.InitType, 0, Array.Empty<KitbagSlot>());
        private static readonly ShortcutSlot[] NoShortcuts = Array.Empty<ShortcutSlot>();
        private static readonly Boat[] NoBoats = Array.Empty<Boat>();

        public EnterMapReply(SwitchMap switchMap, bool isAutoLocked, bool isKitbagLocked, int imps,
            ActorCreate actor, short defaultSkillId, byte skillBagType, IReadOnlyList<SkillSlot> skillBag,
            int serverTime, IReadOnlyList<SkillState> skillStates, byte attributeType,
            IReadOnlyList<CharacterAttribute> attributes, KitbagSync kitbag, IReadOnlyList<ShortcutSlot> shortcuts,
            IReadOnlyList<Boat> boats, uint mainCharacterId)
        {
            SwitchMap = switchMap ?? throw new ArgumentNullException(nameof(switchMap));

            if (switchMap.IsRefused)
            {
                throw new ArgumentException("A refused reply carries its code and nothing else.", nameof(switchMap));
            }

            IsAutoLocked = isAutoLocked;
            IsKitbagLocked = isKitbagLocked;
            Imps = imps;
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            DefaultSkillId = defaultSkillId;
            SkillBagType = skillBagType;
            SkillBag = skillBag ?? throw new ArgumentNullException(nameof(skillBag));
            ServerTime = serverTime;
            SkillStates = skillStates ?? throw new ArgumentNullException(nameof(skillStates));
            AttributeType = attributeType;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Kitbag = kitbag ?? throw new ArgumentNullException(nameof(kitbag));
            Shortcuts = shortcuts ?? throw new ArgumentNullException(nameof(shortcuts));
            Boats = boats ?? throw new ArgumentNullException(nameof(boats));
            MainCharacterId = mainCharacterId;
        }

        private EnterMapReply(SwitchMap switchMap)
        {
            SwitchMap = switchMap;
            Actor = null;
            SkillBag = NoSkills;
            SkillStates = NoSkillStates;
            Attributes = NoAttributes;
            Kitbag = NoKitbag;
            Shortcuts = NoShortcuts;
            Boats = NoBoats;
        }

        public SwitchMap SwitchMap { get; }

        public ushort Code => SwitchMap.Code;

        public bool IsRefused => SwitchMap.IsRefused;

        // TODO: the world UI's kitbag publishes the auto-lock flag.
        public bool IsAutoLocked { get; }

        public bool IsKitbagLocked { get; }

        // TODO: the world UI's kitbag publishes the IMP count.
        public int Imps { get; }

        public ActorCreate Actor { get; }

        // TODO: the world UI's shortcut bar publishes the skill bag.
        public short DefaultSkillId { get; }

        // TODO: the world UI's shortcut bar publishes the skill bag.
        public byte SkillBagType { get; }

        // TODO: the world UI's shortcut bar publishes the skill bag.
        public IReadOnlyList<SkillSlot> SkillBag { get; }

        // TODO: the world UI publishes the server time to time the skill states.
        public int ServerTime { get; }

        // TODO: the world UI publishes the skill states.
        public IReadOnlyList<SkillState> SkillStates { get; }

        // TODO: the world UI publishes the attribute sync type.
        public byte AttributeType { get; }

        public IReadOnlyList<CharacterAttribute> Attributes { get; }

        public KitbagSync Kitbag { get; }

        // TODO: the world UI's shortcut bar publishes the shortcut bar.
        public IReadOnlyList<ShortcutSlot> Shortcuts { get; }

        // TODO: boats publish the boat loop.
        public IReadOnlyList<Boat> Boats { get; }

        // TODO: boats publish the main-character id.
        public uint MainCharacterId { get; }

        public static EnterMapReply Refused(ushort code)
        {
            return new EnterMapReply(SwitchMap.Refused(code));
        }

        public static EnterMapReply Read(PacketReader packet)
        {
            return Read(packet, KitbagSlot.DefaultBoatItemIds);
        }

        public static EnterMapReply Read(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            if (boatItemIds == null)
            {
                throw new ArgumentNullException(nameof(boatItemIds));
            }

            var code = packet.ReadUShort();

            if (code != 0)
            {
                return Refused(code);
            }

            var isAutoLocked = packet.ReadByte() != 0;
            var isKitbagLocked = packet.ReadByte() != 0;
            var switchMap = SwitchMap.ReadBody(packet);
            var imps = packet.ReadInt();
            var actor = ActorCreate.Read(packet);
            var defaultSkillId = packet.ReadShort();
            var skillBagType = packet.ReadByte();
            var skillBag = SkillSlot.ReadList(packet);
            var serverTime = packet.ReadInt();
            var skillStates = SkillState.ReadList(packet);
            var attributeType = packet.ReadByte();
            var attributes = CharacterAttribute.ReadList(packet);
            var kitbag = KitbagSync.Read(packet, boatItemIds);
            var shortcuts = ShortcutSlot.ReadList(packet);
            var boats = ReadBoats(packet, boatItemIds);

            return new EnterMapReply(switchMap, isAutoLocked, isKitbagLocked, imps, actor, defaultSkillId,
                skillBagType, skillBag, serverTime, skillStates, attributeType, attributes, kitbag, shortcuts, boats,
                packet.ReadUInt());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUShort(Code);

            if (IsRefused)
            {
                return;
            }

            packet.WriteByte(IsAutoLocked ? (byte)1 : (byte)0);
            packet.WriteByte(IsKitbagLocked ? (byte)1 : (byte)0);
            SwitchMap.WriteBody(packet);
            packet.WriteInt(Imps);
            Actor.Write(packet);
            packet.WriteShort(DefaultSkillId);
            packet.WriteByte(SkillBagType);
            SkillSlot.WriteList(packet, SkillBag);
            packet.WriteInt(ServerTime);
            SkillState.WriteList(packet, SkillStates);
            packet.WriteByte(AttributeType);
            CharacterAttribute.WriteList(packet, Attributes);
            Kitbag.Write(packet);
            ShortcutSlot.WriteList(packet, Shortcuts);
            WriteBoats(packet);
            packet.WriteUInt(MainCharacterId);
        }

        private static IReadOnlyList<Boat> ReadBoats(PacketReader packet, IReadOnlyCollection<short> boatItemIds)
        {
            var count = packet.ReadByte();
            var boats = new Boat[count];

            for (var i = 0; i < boats.Length; i++)
            {
                boats[i] = Boat.Read(packet, boatItemIds);
            }

            return boats;
        }

        private void WriteBoats(PacketWriter packet)
        {
            if (Boats.Count > byte.MaxValue)
            {
                throw new InvalidDataException($"A list of {Boats.Count} boats does not fit a one-byte count.");
            }

            packet.WriteByte((byte)Boats.Count);

            foreach (var boat in Boats)
            {
                boat.Write(packet);
            }
        }
    }
}
