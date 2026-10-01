using Godot;
using Top.Client.Core;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.Messages;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.UI;
using Top.Logging;

namespace Top.Client.Features.MainMenu.Login;

public partial class LoginScreen : Screen
{
    private const int ShortestPasswordLength = 5;

    private const double ConnectingDelaySeconds = 0.2;

    private IServerConnection _connection = null!;

    private ServerEntry _server = null!;

    private bool _isAttempting;

    private Tween? _connectingDelay;

    [Signal]
    public delegate void LoggedInEventHandler();

    [Export] public StepBlock Block { get; set; } = null!;

    [Export] public Control FormPage { get; set; } = null!;

    [Export] public LineEdit AccountEdit { get; set; } = null!;

    [Export] public LineEdit PasswordEdit { get; set; } = null!;

    [Export] public Button CreateAccountRow { get; set; } = null!;

    [Export] public Button LogInButton { get; set; } = null!;

    [Export] public Button BackButton { get; set; } = null!;

    [Export] public Control ConnectingPage { get; set; } = null!;

    [Export] public Label ConnectingLabel { get; set; } = null!;

    [Export] public Button CancelButton { get; set; } = null!;

    [Export] public MessagePopup MessagePopup { get; set; } = null!;

    public LoginSucceededEvent? Login { get; private set; }

    public string AccountName => AccountEdit.Text;

    protected override Control NavigationStart => AccountEdit.Text.Length > 0 ? PasswordEdit : AccountEdit;

    public void Setup(IServerConnection connection, ServerEntry server, string accountName)
    {
        _connection = connection;
        _server = server;

        AccountEdit.Text = accountName;
    }

    public override void _Ready()
    {
        Block.ServerLine.ShowServer(_server.Name, _server.Address);

        AccountEdit.TextChanged += _ => UpdateLogInButton();
        PasswordEdit.TextChanged += _ => UpdateLogInButton();
        AccountEdit.TextSubmitted += _ => LogIn();
        PasswordEdit.TextSubmitted += _ => LogIn();

        LogInButton.Pressed += LogIn;
        BackButton.Pressed += GoBack;
        CancelButton.Pressed += CancelLogIn;
        MessagePopup.PopupHide += FocusPassword;

        Block.AcceptRequested += LogIn;
        Block.CancelRequested += CancelOrGoBack;

        Subscribe<LoginSucceededEvent>(_connection, CompleteLogin);
        Subscribe<LoginFailedEvent>(_connection, ShowFailure);
        Subscribe<DisconnectedEvent>(_connection, ShowDisconnect);

        UpdateLogInButton();
        SetupFocus();
    }

    private bool CanLogIn()
    {
        return AccountEdit.Text.Length > 0 && PasswordEdit.Text.Length >= ShortestPasswordLength;
    }

    private void UpdateLogInButton()
    {
        LogInButton.Disabled = !CanLogIn();
    }

    private void LogIn()
    {
        if (_isAttempting || !CanLogIn())
        {
            return;
        }

        BeginAttempt();

        _connection.Connect(_server.Host, _server.Port, isEncrypted: true);
        _connection.Send(new LogInCommand(AccountEdit.Text, PasswordEdit.Text));
    }

    private void CancelOrGoBack()
    {
        if (_isAttempting)
        {
            CancelLogIn();
        }
        else
        {
            GoBack();
        }
    }

    private void CancelLogIn()
    {
        EndAttempt();

        _connection.Close();

        FocusPassword();
    }

    private void GoBack()
    {
        EndAttempt();

        _connection.Close();

        Close();
    }

    private void CompleteLogin(LoginSucceededEvent accepted)
    {
        if (!_isAttempting)
        {
            return;
        }

        EndAttempt();

        Log.Info($"logged in to {_server} as {AccountEdit.Text}");

        Login = accepted;

        EmitSignal(SignalName.LoggedIn);
    }

    private void ShowFailure(LoginFailedEvent failed)
    {
        if (!_isAttempting)
        {
            return;
        }

        PasswordEdit.Text = string.Empty;

        UpdateLogInButton();

        EndAttempt();

        _connection.Close();

        var text = failed.Text.Length > 0 ? failed.Text : "The server refused the log in.";

        MessagePopup.Open("Log in failed", text, failed.Code);
    }

    private void ShowDisconnect(DisconnectedEvent disconnected)
    {
        if (!_isAttempting)
        {
            return;
        }

        EndAttempt();

        MessagePopup.Open(Disconnection.Title, Disconnection.ToText(disconnected.Reason));
    }

    private void FocusPassword()
    {
        PasswordEdit.Edit();
    }

    private void BeginAttempt()
    {
        _isAttempting = true;

        _connectingDelay = CreateTween();
        _connectingDelay.TweenInterval(ConnectingDelaySeconds);
        _connectingDelay.TweenCallback(Callable.From(ShowConnecting));
    }

    private void EndAttempt()
    {
        _isAttempting = false;

        _connectingDelay?.Kill();

        Block.ServerLine.ShowAccount(null);
        Block.ShowPage(FormPage);
    }

    private void ShowConnecting()
    {
        ConnectingLabel.Text = $"Logging in as {AccountEdit.Text}…";

        Block.ServerLine.ShowAccount(AccountEdit.Text);
        Block.ShowPage(ConnectingPage);
    }

    private void SetupFocus()
    {
        StackFocus(AccountEdit, PasswordEdit, CreateAccountRow, LogInButton);
        ChainFocus(CreateAccountRow, BackButton, LogInButton);

        BackButton.FocusNeighborTop = BackButton.GetPathTo(CreateAccountRow);
        BackButton.FocusNeighborRight = BackButton.GetPathTo(LogInButton);

        LogInButton.FocusNeighborLeft = LogInButton.GetPathTo(BackButton);
    }
}
