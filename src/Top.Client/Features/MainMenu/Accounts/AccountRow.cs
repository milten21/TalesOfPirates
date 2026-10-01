using System;
using Godot;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.UI;

namespace Top.Client.Features.MainMenu.Accounts;

public partial class AccountRow : ConfirmRow
{
    [Signal]
    public delegate void ForgetPressedEventHandler();

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Label LastLoginLabel { get; set; } = null!;

    [Export] public Button ForgetButton { get; set; } = null!;

    public AccountEntry Account { get; private set; } = null!;

    public void Setup(AccountEntry account, ButtonGroup group, DateTime now)
    {
        Account = account;

        Row.ButtonGroup = group;

        NameLabel.Text = account.Name;

        LastLoginLabel.Visible = account.LastLogin.HasValue;
        LastLoginLabel.Text = account.LastLogin is { } lastLogin
            ? $"Last login {RelativeTime.Describe(lastLogin, now)}"
            : string.Empty;
    }

    public override void _Ready()
    {
        base._Ready();

        ForgetButton.Pressed += () => EmitSignal(SignalName.ForgetPressed);

        Row.FocusNeighborRight = Row.GetPathTo(ForgetButton);
        ForgetButton.FocusNeighborLeft = ForgetButton.GetPathTo(Row);
    }

    public void AskToForget()
    {
        Ask($"Forget {Account.Name} on this device?");
    }
}
