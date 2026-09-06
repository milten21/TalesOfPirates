using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Top.Legacy.Protocol.Crypto
{
    public class PacketCipher
    {
        public const int KeySize = 16;

        public const int IvSize = 16;

        private const int TagBits = 96;

        private readonly byte[] _key;
        private readonly SecureRandom _random = new SecureRandom();

        public PacketCipher(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length != KeySize)
            {
                throw new ArgumentException($"The connection key is {KeySize} bytes.", nameof(key));
            }

            _key = key;
        }

        public static byte[] GenerateKey()
        {
            var key = new byte[KeySize];
            new SecureRandom().NextBytes(key);

            return key;
        }

        public byte[] Encrypt(byte[] packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            var iv = new byte[IvSize];
            _random.NextBytes(iv);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new AeadParameters(new KeyParameter(_key), TagBits, iv));

            var ciphertext = new byte[cipher.GetOutputSize(packet.Length)];
            cipher.DoFinal(ciphertext, cipher.ProcessBytes(packet, 0, packet.Length, ciphertext, 0));

            var text = Encoding.ASCII.GetBytes(Convert.ToBase64String(ciphertext));
            var encrypted = new byte[text.Length + 1 + IvSize];
            Array.Copy(text, 0, encrypted, 0, text.Length);
            encrypted[text.Length] = 0x00;
            Array.Copy(iv, 0, encrypted, text.Length + 1, IvSize);

            return encrypted;
        }

        public byte[] Decrypt(byte[] encrypted)
        {
            if (encrypted == null)
            {
                throw new ArgumentNullException(nameof(encrypted));
            }

            if (encrypted.Length <= IvSize + 1)
            {
                throw new InvalidDataException($"An encrypted packet of {encrypted.Length} bytes carries no ciphertext.");
            }

            var textLength = encrypted.Length - IvSize - 1;
            var iv = new byte[IvSize];
            Array.Copy(encrypted, textLength + 1, iv, 0, IvSize);

            byte[] ciphertext;

            try
            {
                ciphertext = Convert.FromBase64String(Encoding.ASCII.GetString(encrypted, 0, textLength));
            }
            catch (FormatException failure)
            {
                throw new InvalidDataException("An encrypted packet is not Base64.", failure);
            }

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new AeadParameters(new KeyParameter(_key), TagBits, iv));

            var packet = new byte[cipher.GetOutputSize(ciphertext.Length)];

            try
            {
                cipher.DoFinal(packet, cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, packet, 0));
            }
            catch (InvalidCipherTextException failure)
            {
                throw new InvalidDataException("An encrypted packet failed its AES-GCM tag check.", failure);
            }

            return packet;
        }
    }
}
