using System.IO;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Top.Legacy.Protocol.Crypto;

namespace Top.Legacy.Protocol.Tests
{
    public class ServerPublicKeyTests
    {
        private AsymmetricCipherKeyPair _gateKeys;
        private byte[] _der;

        [OneTimeSetUp]
        public void GenerateGateKeys()
        {
            var exponent = BigInteger.ValueOf(65537);
            var generator = new RsaKeyPairGenerator();
            generator.Init(new RsaKeyGenerationParameters(exponent, new SecureRandom(), 3072, 100));
            _gateKeys = generator.GenerateKeyPair();
            _der = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(_gateKeys.Public).GetDerEncoded();
        }

        [Test]
        public void A_wrapped_key_is_the_key_the_gate_unwraps()
        {
            var key = PacketCipher.GenerateKey();

            Assert.That(Unwrap(ServerPublicKey.Import(_der).WrapKey(key)), Is.EqualTo(key));
        }

        [Test]
        public void Each_wrap_of_the_same_key_differs()
        {
            var publicKey = ServerPublicKey.Import(_der);
            var key = PacketCipher.GenerateKey();

            Assert.That(publicKey.WrapKey(key), Is.Not.EqualTo(publicKey.WrapKey(key)));
        }

        [Test]
        public void A_wrapped_key_fills_the_modulus()
        {
            Assert.That(
                ServerPublicKey.Import(_der).WrapKey(PacketCipher.GenerateKey()),
                Has.Length.EqualTo(384),
                "the 3072 bits the gate generates");
        }

        [Test]
        public void Bytes_that_are_not_der_are_refused()
        {
            Assert.That(
                () => ServerPublicKey.Import(new byte[] { 0x21, 0x21, 0x21, 0x21 }),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void Der_that_is_not_an_rsa_key_is_refused()
        {
            Assert.That(
                () => ServerPublicKey.Import(new byte[] { 0x30, 0x03, 0x02, 0x01, 0x00 }),
                Throws.InstanceOf<InvalidDataException>());
        }

        private byte[] Unwrap(byte[] wrapped)
        {
            var oaep = new OaepEncoding(new RsaEngine(), new Sha1Digest());
            oaep.Init(false, _gateKeys.Private);

            return oaep.ProcessBlock(wrapped, 0, wrapped.Length);
        }
    }
}
