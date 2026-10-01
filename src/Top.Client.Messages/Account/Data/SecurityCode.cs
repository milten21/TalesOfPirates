using System.Linq;

namespace Top.Client.Messages.Account.Data
{
    public static class SecurityCode
    {
        public const int ShortestLength = 6;

        public const int LongestLength = 12;

        public static bool IsValid(string code)
        {
            return code != null && code.Length >= ShortestLength && code.Length <= LongestLength
                && code.All(IsDigit);
        }

        public static bool IsDigit(char character)
        {
            return character >= '0' && character <= '9';
        }
    }
}
