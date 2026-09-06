using System.Globalization;
using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Top.Legacy.Text;

namespace Top.Legacy.Protocol.Crypto
{
    public static class PasswordDigest
    {
        private const int DigestBits = 256;

        public static string Hex(string password)
        {
            var bytes = Gbk.GetBytes(password ?? string.Empty);

            var digest = new Blake2sDigest(DigestBits);
            digest.BlockUpdate(bytes, 0, bytes.Length);

            var hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);

            var hex = new StringBuilder(hash.Length * 2);

            foreach (var value in hash)
            {
                hex.Append(value.ToString("X2", CultureInfo.InvariantCulture));
            }

            return hex.ToString();
        }
    }
}
