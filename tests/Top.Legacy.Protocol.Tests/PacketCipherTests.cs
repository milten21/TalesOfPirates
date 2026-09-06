using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Protocol.Crypto;

namespace Top.Legacy.Protocol.Tests
{
    public class PacketCipherTests
    {
        private const string KeyHex = "000102030405060708090A0B0C0D0E0F";
        private const string IvHex = "101112131415161718191A1B1C1D1E1F";
        private const string PacketHex = "0001000000000003616200";
        private const string CiphertextBase64 = "xZ/3tn2yJIu3esPq9s3hjnn+AmMSKOs=";

        [Test]
        public void A_pinned_encrypted_packet_decrypts_to_its_packet()
        {
            Assert.That(
                Cipher().Decrypt(PinnedEncryptedPacket()),
                Is.EqualTo(Bytes(PacketHex)),
                "AES-128-GCM with a 16-byte IV and a 12-byte tag, cross-checked against OpenSSL");
        }

        [Test]
        public void An_encrypted_packet_decrypts_back()
        {
            var cipher = Cipher();
            var packet = Bytes(PacketHex);

            Assert.That(cipher.Decrypt(cipher.Encrypt(packet)), Is.EqualTo(packet));
        }

        [Test]
        public void An_empty_packet_encrypts_and_decrypts()
        {
            var cipher = Cipher();

            Assert.That(cipher.Decrypt(cipher.Encrypt(Array.Empty<byte>())), Is.Empty);
        }

        [Test]
        public void An_encrypted_packet_is_base64_then_a_zero_byte_then_the_iv()
        {
            var encrypted = Cipher().Encrypt(Bytes(PacketHex));
            var textLength = encrypted.Length - PacketCipher.IvSize - 1;

            Assert.That(encrypted[textLength], Is.Zero);
            Assert.That(
                Convert.FromBase64String(Encoding.ASCII.GetString(encrypted, 0, textLength)),
                Has.Length.EqualTo(Bytes(PacketHex).Length + 12));
        }

        [Test]
        public void Each_encryption_draws_a_new_iv()
        {
            var cipher = Cipher();
            var packet = Bytes(PacketHex);

            Assert.That(cipher.Encrypt(packet), Is.Not.EqualTo(cipher.Encrypt(packet)));
        }

        [Test]
        public void A_generated_key_is_sixteen_bytes()
        {
            Assert.That(PacketCipher.GenerateKey(), Has.Length.EqualTo(PacketCipher.KeySize));
        }

        [Test]
        public void Each_generated_key_differs()
        {
            Assert.That(PacketCipher.GenerateKey(), Is.Not.EqualTo(PacketCipher.GenerateKey()));
        }

        [Test]
        public void A_key_of_the_wrong_size_is_refused()
        {
            Assert.That(() => new PacketCipher(new byte[8]), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void A_tampered_encrypted_packet_fails_to_decrypt()
        {
            var encrypted = PinnedEncryptedPacket();
            encrypted[3] ^= 0x01;

            Assert.That(() => Cipher().Decrypt(encrypted), Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void Another_key_fails_to_decrypt()
        {
            var other = new PacketCipher(Bytes("0F0E0D0C0B0A09080706050403020100"));

            Assert.That(() => other.Decrypt(PinnedEncryptedPacket()), Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void An_encrypted_packet_with_no_room_for_ciphertext_is_refused()
        {
            Assert.That(
                () => Cipher().Decrypt(new byte[PacketCipher.IvSize + 1]),
                Throws.InstanceOf<InvalidDataException>());
        }

        [Test]
        public void An_encrypted_packet_that_is_not_base64_is_refused()
        {
            var encrypted = new byte[4 + 1 + PacketCipher.IvSize];

            for (var i = 0; i < 4; i++)
            {
                encrypted[i] = (byte)'!';
            }

            Assert.That(() => Cipher().Decrypt(encrypted), Throws.InstanceOf<InvalidDataException>());
        }

        private static PacketCipher Cipher()
        {
            return new PacketCipher(Bytes(KeyHex));
        }

        private static byte[] PinnedEncryptedPacket()
        {
            var text = Encoding.ASCII.GetBytes(CiphertextBase64);
            var encrypted = new byte[text.Length + 1 + PacketCipher.IvSize];
            text.CopyTo(encrypted, 0);
            Bytes(IvHex).CopyTo(encrypted, text.Length + 1);

            return encrypted;
        }

        private static byte[] Bytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }
}
