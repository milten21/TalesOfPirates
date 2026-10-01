using System;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Messages.Account.Commands
{
    public class ChangeSecurityCodeCommand : IServerCommand
    {
        public ChangeSecurityCodeCommand(string currentCode, string newCode)
        {
            if (!SecurityCode.IsValid(newCode))
            {
                throw new ArgumentException(
                    $"A security code has {SecurityCode.ShortestLength} to {SecurityCode.LongestLength} digits.",
                    nameof(newCode));
            }

            CurrentCode = currentCode ?? throw new ArgumentNullException(nameof(currentCode));
            NewCode = newCode;
        }

        public string CurrentCode { get; }

        public string NewCode { get; }
    }
}
