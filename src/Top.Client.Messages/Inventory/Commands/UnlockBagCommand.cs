using System;
using Top.Client.Messages.Account.Data;

namespace Top.Client.Messages.Inventory.Commands
{
    public class UnlockBagCommand : IServerCommand
    {
        public UnlockBagCommand(string code)
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
