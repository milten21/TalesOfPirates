using System;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Top.Legacy.Protocol.Packets;

namespace Top.Legacy.Protocol.Crypto
{
    public class ServerPublicKey
    {
        private readonly AsymmetricKeyParameter _key;
        private readonly SecureRandom _random = new SecureRandom();

        private ServerPublicKey(AsymmetricKeyParameter key)
        {
            _key = key;
        }

        public static ServerPublicKey Import(byte[] der)
        {
            if (der == null)
            {
                throw new ArgumentNullException(nameof(der));
            }

            try
            {
                return new ServerPublicKey(PublicKeyFactory.CreateKey(der));
            }
            catch (Exception failure)
            {
                throw new InvalidDataException("The server public key is not a readable RSA key.", failure);
            }
        }

        public static ServerPublicKey Read(PacketReader packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            packet.ReadUShort();

            return Import(packet.ReadSequence());
        }

        public byte[] WrapKey(byte[] sessionKey)
        {
            if (sessionKey == null)
            {
                throw new ArgumentNullException(nameof(sessionKey));
            }

            var oaep = new OaepEncoding(new RsaEngine(), new Sha1Digest());
            oaep.Init(true, new ParametersWithRandom(_key, _random));

            return oaep.ProcessBlock(sessionKey, 0, sessionKey.Length);
        }
    }
}
