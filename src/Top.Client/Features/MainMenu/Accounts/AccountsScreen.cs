using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.UI;

namespace Top.Client.Features.MainMenu.Accounts;

public partial class AccountsScreen : Screen
{
    private readonly ButtonGroup _rowGroup = new();

    private readonly List<AccountRow> _rows = [];

    private ServerStore _serverStore = null!;

    private ServerEntry _server = null!;

    private AccountEntry? _selected;

    [Signal]
    public delegate void AccountChosenEventHandler(string name);

    [Signal]
    public delegate void NewAccountChosenEventHandler();

    [Export] public PackedScene AccountRowScene { get; set; } = null!;

    [Export] public StepBlock Block { get; set; } = null!;

    [Export] public ScrollList AccountList { get; set; } = null!;

    [Export] public Button OtherAccountRow { get; set; } = null!;

    [Export] public Button BackButton { get; set; } = null!;

    [Export] public Button NextButton { get; set; } = null!;

    protected override Control NavigationStart =>
        _rows.ElementAtOrDefault(Math.Max(FindRowIndex(_selected), 0))?.Row ?? OtherAccountRow;

    public void Setup(ServerStore serverStore, ServerEntry server)
    {
        _serverStore = serverStore;
        _server = server;
    }

    public override void _Ready()
    {
        Block.ServerLine.ShowServer(_server.Name, _server.Address);

        OtherAccountRow.Pressed += ChooseNewAccount;
        BackButton.Pressed += Close;
        NextButton.Pressed += ChooseSelected;

        Block.AcceptRequested += Accept;
        Block.CancelRequested += CancelOrGoBack;

        _selected = _server.LastAccount;

        ShowAccounts();

        if (_selected is null && _rows.Count > 0)
        {
            _rows[0].Select();
        }
    }

    private void ShowAccounts()
    {
        foreach (AccountRow row in _rows)
        {
            row.Visible = false;
            row.QueueFree();
        }

        _rows.Clear();

        DateTime now = DateTime.Now;

        foreach (AccountEntry account in _server.Accounts.OrderBy(account => account.Name, StringComparer.Ordinal))
        {
            _rows.Add(AddAccountRow(account, now));
        }

        if (FindRowIndex(_selected) < 0)
        {
            _selected = null;
        }

        UpdateNextButton();
        SetupFocus();
    }

    private AccountRow AddAccountRow(AccountEntry account, DateTime now)
    {
        var row = AccountRowScene.Instantiate<AccountRow>();

        row.Setup(account, _rowGroup, now);

        if (account == _selected)
        {
            row.Select();
        }

        AccountList.Items.AddChild(row);

        row.Selected += () => Select(account);
        row.Activated += () => Choose(account);
        row.ForgetPressed += () => AskToForget(row);
        row.Confirmed += () => Forget(account, row.WasConfirmOpenedWithKeyboard);

        return row;
    }

    private void Select(AccountEntry account)
    {
        _selected = account;

        CloseConfirmsExcept(account);
        UpdateNextButton();
    }

    private void CloseConfirmsExcept(AccountEntry? account)
    {
        foreach (AccountRow row in _rows.Where(row => row.Account != account))
        {
            row.CloseConfirm();
        }
    }

    private void UpdateNextButton()
    {
        NextButton.Disabled = _selected is null;
    }

    private void AskToForget(AccountRow row)
    {
        CloseConfirmsExcept(row.Account);

        row.AskToForget();
    }

    private void Forget(AccountEntry account, bool shouldFocusNeighbour)
    {
        int index = FindRowIndex(account);
        AccountEntry? neighbour = (_rows.ElementAtOrDefault(index + 1) ?? _rows.ElementAtOrDefault(index - 1))?.Account;

        _serverStore.ForgetAccount(_server, account.Name);

        if (_server.Accounts.Count == 0)
        {
            ChooseNewAccount();

            return;
        }

        if (_server.Accounts.Count == 1)
        {
            Choose(_server.Accounts[0]);

            return;
        }

        _selected = neighbour;

        ShowAccounts();

        if (shouldFocusNeighbour)
        {
            FocusRowOrOtherAccountRow(FindRowIndex(neighbour));
        }
    }

    private void Accept()
    {
        if (!_rows.Any(row => row.IsConfirming))
        {
            ChooseSelected();
        }
    }

    private void CancelOrGoBack()
    {
        if (_rows.FirstOrDefault(row => row.IsConfirming) is { } row)
        {
            row.Keep();
        }
        else
        {
            Close();
        }
    }

    private void ChooseSelected()
    {
        if (_selected is { } account)
        {
            Choose(account);
        }
    }

    private void Choose(AccountEntry account)
    {
        EmitSignal(SignalName.AccountChosen, account.Name);
    }

    private void ChooseNewAccount()
    {
        EmitSignal(SignalName.NewAccountChosen);
    }

    private int FindRowIndex(AccountEntry? account)
    {
        return _rows.FindIndex(row => row.Account == account);
    }

    private void FocusRowOrOtherAccountRow(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            OtherAccountRow.GrabFocus();

            return;
        }

        _rows[index].Row.GrabFocus();
    }

    private void SetupFocus()
    {
        List<Control> order = [];

        for (int index = 0; index < _rows.Count; index++)
        {
            AccountRow row = _rows[index];
            AccountRow? upper = index > 0 ? _rows[index - 1] : null;
            AccountRow? lower = index < _rows.Count - 1 ? _rows[index + 1] : null;

            LinkVertically(row.Row, upper?.Row, lower?.Row ?? OtherAccountRow);
            LinkVertically(row.ForgetButton, upper?.ForgetButton, lower?.ForgetButton ?? OtherAccountRow);

            LinkVertically(row.InlineConfirm.ConfirmButton, row.Row, lower?.Row ?? OtherAccountRow);
            LinkVertically(row.InlineConfirm.KeepButton, row.Row, lower?.Row ?? OtherAccountRow);

            order.AddRange([row.Row, row.ForgetButton]);
        }

        LinkVertically(OtherAccountRow, _rows.Count > 0 ? _rows[^1].Row : null, NextButton);
        LinkVertically(BackButton, OtherAccountRow, null);
        LinkVertically(NextButton, OtherAccountRow, null);

        LinkHorizontally(BackButton, NextButton);

        order.AddRange([OtherAccountRow, BackButton, NextButton]);

        ChainFocus([.. order]);
    }
}
