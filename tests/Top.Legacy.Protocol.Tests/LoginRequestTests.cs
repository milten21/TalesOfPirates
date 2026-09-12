using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Protocol.Messages.Login;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Tests
{
    public class LoginRequestTests
    {
        private const string Account = "MiltenUnityPort";
        private const string Password = "secret";
        private const string SecretDigest = "66E754709229A1A76F12B770D612D4DBA1D51E28894E2DCE1B53CA15104F84C0";
        private const string UnknownMacAddress = "Unknown";
        private const ushort DefaultMark = 911;
        private const ushort ForgedMark = 0;
        private const ushort ClientVersion = 32125;

        [Test]
        public void A_request_round_trips()
        {
            var read = LoginRequest.Read(Reader(new LoginRequest(Account, Password, ClientVersion)));

            Assert.That(read.Account, Is.EqualTo(Account));
            Assert.That(read.Digest, Is.EqualTo(SecretDigest));
            Assert.That(read.MacAddress, Is.EqualTo(UnknownMacAddress));
            Assert.That(read.AntiCheatMark, Is.EqualTo(DefaultMark));
            Assert.That(read.ClientVersion, Is.EqualTo(ClientVersion));
        }

        [Test]
        public void A_request_is_the_account_the_digest_the_mac_address_the_mark_and_the_version()
        {
            Assert.That(
                Written(new LoginRequest("crew", Password, ClientVersion)),
                Is.EqualTo(Join(
                    Text("crew"),
                    Text(SecretDigest),
                    Text(UnknownMacAddress),
                    BigEndian(DefaultMark),
                    BigEndian(ClientVersion))));
        }

        [Test]
        public void The_password_travels_as_its_blake2s_digest()
        {
            var request = new LoginRequest(Account, Password, ClientVersion);

            Assert.That(request.Digest, Is.EqualTo(SecretDigest), "BLAKE2s-256 over the GBK bytes, uppercase hex");
            Assert.That(
                Encoding.ASCII.GetString(Written(request)),
                Does.Not.Contain(Password),
                "the password itself never reaches the gate");
        }

        [Test]
        public void The_client_version_the_settings_carry_is_the_one_the_request_sends()
        {
            var settings = new GateSettings();

            var read = LoginRequest.Read(Reader(new LoginRequest(Account, Password, settings.ClientVersion)));

            Assert.That(read.ClientVersion, Is.EqualTo(settings.ClientVersion));
        }

        [Test]
        public void A_mark_the_gate_counts_as_cheating_decodes_as_the_value_it_arrived_as()
        {
            var packet = Join(
                Text(Account),
                Text(SecretDigest),
                Text(UnknownMacAddress),
                BigEndian(ForgedMark),
                BigEndian(ClientVersion));

            Assert.That(
                LoginRequest.Read(new PacketReader(packet)).AntiCheatMark,
                Is.EqualTo(ForgedMark),
                "the gate counts anything but the default as cheating, so the value has to survive");
        }

        [Test]
        public void A_request_carrying_a_mark_of_its_own_round_trips()
        {
            var read = LoginRequest.Read(Reader(new LoginRequest(Account, Password, ClientVersion, ForgedMark)));

            Assert.That(read.AntiCheatMark, Is.EqualTo(ForgedMark));
            Assert.That(read.ClientVersion, Is.EqualTo(ClientVersion), "the mark does not shift the version");
        }

        [Test]
        public void A_request_that_names_no_mark_sends_the_default_one()
        {
            Assert.That(
                new LoginRequest(Account, Password, ClientVersion).AntiCheatMark,
                Is.EqualTo(LoginRequest.DefaultMark));
        }

        [Test]
        public void A_request_that_runs_out_of_bytes_throws()
        {
            var written = Written(new LoginRequest(Account, Password, ClientVersion));
            var truncated = new byte[written.Length - 1];
            Array.Copy(written, truncated, truncated.Length);

            Assert.That(
                () => LoginRequest.Read(new PacketReader(truncated)),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void A_request_without_an_account_is_refused()
        {
            Assert.That(
                () => new LoginRequest(null, Password, ClientVersion),
                Throws.InstanceOf<ArgumentNullException>());
        }

        private static PacketReader Reader(LoginRequest request)
        {
            return new PacketReader(Written(request));
        }

        private static byte[] Written(LoginRequest request)
        {
            var packet = new PacketWriter();
            request.Write(packet);

            return packet.ToArray();
        }

        private static byte[] Text(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);

            return Join(BigEndian((ushort)(bytes.Length + 1)), bytes, new byte[] { 0x00 });
        }

        private static byte[] BigEndian(ushort value)
        {
            return new[] { (byte)(value >> 8), (byte)value };
        }

        private static byte[] Join(params byte[][] parts)
        {
            var joined = new List<byte>();

            foreach (var part in parts)
            {
                joined.AddRange(part);
            }

            return joined.ToArray();
        }
    }
}
