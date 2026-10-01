using System;

namespace Top.Client.Messages.Account.Commands
{
    public class LogInCommand : IServerCommand
    {
        public LogInCommand(string account, string password)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }

        public string Account { get; }

        public string Password { get; }
    }
}
