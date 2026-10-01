using System;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Messages.Account.Commands
{
    public class CreateSecurityCodeCommand : IServerCommand
    {
        public CreateSecurityCodeCommand(string code)
        {
            if (!SecurityCode.IsValid(code))
            {
                throw new ArgumentException(
                    $"A security code has {SecurityCode.ShortestLength} to {SecurityCode.LongestLength} digits.",
                    nameof(code));
            }

            Code = code;
        }

        public string Code { get; }
    }
}
