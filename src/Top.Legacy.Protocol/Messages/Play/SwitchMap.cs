using System;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Play
{
    public class SwitchMap
    {
        private const ushort SuccessCode = 0;

        private SwitchMap(ushort code, byte enterType, bool isNewCharacter, string mapName, bool canTeam)
        {
            Code = code;
            EnterType = enterType;
            IsNewCharacter = isNewCharacter;
            MapName = mapName;
            CanTeam = canTeam;
        }

        public ushort Code { get; }

        public byte EnterType { get; }

        // TODO: the world UI publishes the new-character flag.
        public bool IsNewCharacter { get; }

        public string MapName { get; }

        // TODO: teams publish the team flag.
        public bool CanTeam { get; }

        public bool IsRefused => Code != SuccessCode;

        public static SwitchMap Entered(byte enterType, bool isNewCharacter, string mapName, bool canTeam)
        {
            if (mapName == null)
            {
                throw new ArgumentNullException(nameof(mapName));
            }

            return new SwitchMap(SuccessCode, enterType, isNewCharacter, mapName, canTeam);
        }

        public static SwitchMap Refused(ushort code)
        {
            if (code == SuccessCode)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(code), code, "A refused map switch carries a code above zero.");
            }

            return new SwitchMap(code, 0, false, string.Empty, false);
        }

        public static SwitchMap ReadBody(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var enterType = packet.ReadByte();
            var isNewCharacter = packet.ReadByte() != 0;
            var mapName = packet.ReadString();

            return Entered(enterType, isNewCharacter, mapName, packet.ReadByte() != 0);
        }

        public void WriteBody(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteByte(EnterType);
            packet.WriteByte(IsNewCharacter ? (byte)1 : (byte)0);
            packet.WriteString(MapName);
            packet.WriteByte(CanTeam ? (byte)1 : (byte)0);
        }
    }
}
