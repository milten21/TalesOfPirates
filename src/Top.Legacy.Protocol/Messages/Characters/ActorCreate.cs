using System;
using System.Collections.Generic;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Characters
{
    public class ActorCreate
    {
        public ActorCreate(uint characterId, uint worldId, uint communityId, string communityName, byte gmLevel,
            int handle, byte controlType, string name, string motto, short icon, int guildId, string guildName,
            string guildMotto, int guildPermission, string stallName, short existState, Point position, int radius,
            short angle, uint teamLeaderId, bool isPlayer, byte side, EntityEvent entityEvent, LookInfo look,
            byte pkControl, IReadOnlyList<AppendLook> appendLooks)
        {
            if (appendLooks == null)
            {
                throw new ArgumentNullException(nameof(appendLooks));
            }

            if (appendLooks.Count != AppendLook.Slots)
            {
                throw new ArgumentException(
                    $"A character carries {AppendLook.Slots} append looks and {appendLooks.Count} were given.",
                    nameof(appendLooks));
            }

            CharacterId = characterId;
            WorldId = worldId;
            CommunityId = communityId;
            CommunityName = communityName ?? throw new ArgumentNullException(nameof(communityName));
            GmLevel = gmLevel;
            Handle = handle;
            ControlType = controlType;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Motto = motto ?? throw new ArgumentNullException(nameof(motto));
            Icon = icon;
            GuildId = guildId;
            GuildName = guildName ?? throw new ArgumentNullException(nameof(guildName));
            GuildMotto = guildMotto ?? throw new ArgumentNullException(nameof(guildMotto));
            GuildPermission = guildPermission;
            StallName = stallName ?? throw new ArgumentNullException(nameof(stallName));
            ExistState = existState;
            Position = position;
            Radius = radius;
            Angle = angle;
            TeamLeaderId = teamLeaderId;
            IsPlayer = isPlayer;
            Side = side;
            Event = entityEvent ?? throw new ArgumentNullException(nameof(entityEvent));
            Look = look ?? throw new ArgumentNullException(nameof(look));
            PkControl = pkControl;
            AppendLooks = appendLooks;
        }

        public uint CharacterId { get; }

        public uint WorldId { get; }

        // TODO: boats and pets publish the community id.
        public uint CommunityId { get; }

        // TODO: boats and pets publish the community name.
        public string CommunityName { get; }

        // TODO: the chat publishes the GM level.
        public byte GmLevel { get; }

        // TODO: targeting and picking publish the handle.
        public int Handle { get; }

        public byte ControlType { get; }

        public string Name { get; }

        // TODO: the world UI publishes the motto.
        public string Motto { get; }

        // TODO: the world UI's target frame publishes the icon.
        public short Icon { get; }

        public int GuildId { get; }

        public string GuildName { get; }

        public string GuildMotto { get; }

        public int GuildPermission { get; }

        // TODO: stall frames publish the stall name.
        public string StallName { get; }

        public short ExistState { get; }

        public Point Position { get; }

        // TODO: targeting and picking publish the area radius.
        public int Radius { get; }

        public short Angle { get; }

        public uint TeamLeaderId { get; }

        // TODO: the world UI publishes the player flag.
        public bool IsPlayer { get; }

        // TODO: the side and faction branch of the plate rule publishes the side.
        public byte Side { get; }

        // TODO: NPC talk publishes the entity event.
        public EntityEvent Event { get; }

        // TODO: building a player character out of equipment parts publishes the look.
        public LookInfo Look { get; }

        // TODO: the world UI publishes the PK flags.
        public byte PkControl { get; }

        // TODO: building a player character out of equipment parts publishes the append looks.
        public IReadOnlyList<AppendLook> AppendLooks { get; }

        public static ActorCreate Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var characterId = packet.ReadUInt();
            var worldId = packet.ReadUInt();
            var communityId = packet.ReadUInt();
            var communityName = packet.ReadString();
            var gmLevel = packet.ReadByte();
            var handle = packet.ReadInt();
            var controlType = packet.ReadByte();
            var name = packet.ReadString();
            var motto = packet.ReadString();
            var icon = packet.ReadShort();
            var guildId = packet.ReadInt();
            var guildName = packet.ReadString();
            var guildMotto = packet.ReadString();
            var guildPermission = packet.ReadInt();
            var stallName = packet.ReadString();
            var existState = packet.ReadShort();
            var x = packet.ReadInt();
            var y = packet.ReadInt();
            var radius = packet.ReadInt();
            var angle = packet.ReadShort();
            var teamLeaderId = packet.ReadUInt();
            var isPlayer = packet.ReadByte() != 0;
            var side = packet.ReadByte();
            var entityEvent = EntityEvent.Read(packet);
            var look = LookInfo.Read(packet);
            var pkControl = packet.ReadByte();

            return new ActorCreate(characterId, worldId, communityId, communityName, gmLevel, handle, controlType,
                name, motto, icon, guildId, guildName, guildMotto, guildPermission, stallName, existState,
                new Point(x, y), radius, angle, teamLeaderId, isPlayer, side, entityEvent, look, pkControl,
                AppendLook.ReadList(packet));
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteUInt(CharacterId);
            packet.WriteUInt(WorldId);
            packet.WriteUInt(CommunityId);
            packet.WriteString(CommunityName);
            packet.WriteByte(GmLevel);
            packet.WriteInt(Handle);
            packet.WriteByte(ControlType);
            packet.WriteString(Name);
            packet.WriteString(Motto);
            packet.WriteShort(Icon);
            packet.WriteInt(GuildId);
            packet.WriteString(GuildName);
            packet.WriteString(GuildMotto);
            packet.WriteInt(GuildPermission);
            packet.WriteString(StallName);
            packet.WriteShort(ExistState);
            packet.WriteInt(Position.X);
            packet.WriteInt(Position.Y);
            packet.WriteInt(Radius);
            packet.WriteShort(Angle);
            packet.WriteUInt(TeamLeaderId);
            packet.WriteByte(IsPlayer ? (byte)1 : (byte)0);
            packet.WriteByte(Side);
            Event.Write(packet);
            Look.Write(packet);
            packet.WriteByte(PkControl);
            AppendLook.WriteList(packet, AppendLooks);
        }
    }
}
