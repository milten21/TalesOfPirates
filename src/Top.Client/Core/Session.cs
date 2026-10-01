using Top.Client.Features.MainMenu.Servers;
using Top.Client.Messages.Account.Events;

namespace Top.Client.Core;

public class Session
{
    public ServerEntry? CurrentServer { get; set; }

    public string? CurrentAccountName { get; set; }

    public LoginSucceededEvent? CurrentLogin { get; set; }
}
