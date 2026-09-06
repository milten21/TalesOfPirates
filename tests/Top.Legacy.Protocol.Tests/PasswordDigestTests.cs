using NUnit.Framework;
using Top.Legacy.Protocol.Crypto;

namespace Top.Legacy.Protocol.Tests
{
    public class PasswordDigestTests
    {
        [Test]
        public void An_empty_password_digests_to_the_vector_for_no_input()
        {
            Assert.That(
                PasswordDigest.Hex(string.Empty),
                Is.EqualTo("69217A3079908094E11121D042354A7C1F55B6482CA1A51E1B250DFD1ED0EEF9"),
                "RFC 7693 BLAKE2s-256 of the empty input");
        }

        [Test]
        public void An_ascii_password_digests_to_the_vector_for_its_bytes()
        {
            Assert.That(
                PasswordDigest.Hex("abc"),
                Is.EqualTo("508C5E8C327C14E2E1A72BA34EEB452F37458B209ED63A294D999B4C86675982"),
                "RFC 7693 BLAKE2s-256 of abc");
        }

        [Test]
        public void A_password_digests_over_its_gbk_bytes()
        {
            Assert.That(
                PasswordDigest.Hex("密码"),
                Is.EqualTo("4A1B92A37FF503C5E11E0737FC9681F1B15A296DDCE2D67E81A7DDAADB4C8FF3"),
                "BLAKE2s-256 of C3DCC2EB, the GBK bytes of 密码");
        }

        [Test]
        public void A_null_password_digests_as_an_empty_one()
        {
            Assert.That(PasswordDigest.Hex(null), Is.EqualTo(PasswordDigest.Hex(string.Empty)));
        }

        [Test]
        public void A_digest_is_uppercase_hex_of_thirty_two_bytes()
        {
            Assert.That(PasswordDigest.Hex("24022402"), Does.Match("^[0-9A-F]{64}$"));
        }
    }
}
