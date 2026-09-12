using System;
using Top.Legacy.Protocol.Crypto;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Messages.Login
{
    public class LoginRequest
    {
        public const ushort DefaultMark = 911;

        private const string UnknownMacAddress = "Unknown";

        public LoginRequest(
            string account, string password, ushort clientVersion, ushort antiCheatMark = DefaultMark)
            : this(account, PasswordDigest.Hex(password), UnknownMacAddress, antiCheatMark, clientVersion)
        {
        }

        private LoginRequest(
            string account, string digest, string macAddress, ushort antiCheatMark, ushort clientVersion)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Digest = digest;
            MacAddress = macAddress;
            AntiCheatMark = antiCheatMark;
            ClientVersion = clientVersion;
        }

        public string Account { get; }

        public string Digest { get; }

        public string MacAddress { get; }

        public ushort AntiCheatMark { get; }

        public ushort ClientVersion { get; }

        public static LoginRequest Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var account = packet.ReadString();
            var digest = packet.ReadString();
            var macAddress = packet.ReadString();
            var antiCheatMark = packet.ReadUShort();

            return new LoginRequest(account, digest, macAddress, antiCheatMark, packet.ReadUShort());
        }

        public void Write(PacketWriter packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.WriteString(Account);
            packet.WriteString(Digest);
            packet.WriteString(MacAddress);
            packet.WriteUShort(AntiCheatMark);
            packet.WriteUShort(ClientVersion);
        }
    }
}
