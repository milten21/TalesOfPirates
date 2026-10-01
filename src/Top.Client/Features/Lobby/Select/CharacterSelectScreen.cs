using System.Linq;
using Godot;
using Top.Client.Features.Lobby.Select.Forms;
using Top.Client.Features.MainMenu.Servers;
using Top.Client.Features.MainMenu.Servers.Widgets;
using Top.Client.Messages;
using Top.Client.Messages.Account.Commands;
using Top.Client.Messages.Account.Data;
using Top.Client.Messages.Account.Events;
using Top.Client.Messages.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Events;
using Top.Client.UI;
using Top.Logging;

namespace Top.Client.Features.Lobby.Select;

public partial class CharacterSelectScreen : Screen
{
    private const string EnterFailedTitle = "Enter failed";

    private const string EnterWorldCaption = "ENTER WORLD";

    private const string CreateCharacterCaption = "CREATE CHARACTER";

    private const string SecurityCodeSetText = "Security code set";

    private const string SecurityCodeChangedText = "Security code changed";

    private const string WrongCurrentCodeText = "Current code is wrong";

    private const string WrongDeleteCodeText = "Wrong security code";

    private const int GuildLeader = 530;

    private const int ServerBusy = 532;

    private const int WrongSecurityCode = 534;

    private const int CharacterOnline = 538;

    private const float SelectFadeHeight = 300f;

    private const float FormFadeHeight = 520f;

    private const double SceneLookSeconds = 0.3;

    private IServerConnection _connection = null!;

    private State _state = State.Select;

    private bool _hasSelection;

    private bool _isLeaving;

    private int _deletingIndex;

    private string _deletingName = string.Empty;

    private Tween? _sceneLook;

    [Export] public Control BottomFade { get; set; } = null!;

    [Export] public Control Dim { get; set; } = null!;

    [Export] public HoledDim HoledDim { get; set; } = null!;

    [Export] public Label AccountLabel { get; set; } = null!;

    [Export] public ServerLine ServerLine { get; set; } = null!;

    [Export] public Button LogOutButton { get; set; } = null!;

    [Export] public ActionArea ActionArea { get; set; } = null!;

    [Export] public Control SelectActions { get; set; } = null!;

    [Export] public Control EnterWorldLine { get; set; } = null!;

    [Export] public Label EnterWorldLabel { get; set; } = null!;

    [Export] public Button PrimaryPill { get; set; } = null!;

    [Export] public Button CreateButton { get; set; } = null!;

    [Export] public Button SecurityCodeButton { get; set; } = null!;

    [Export] public Button DeleteButton { get; set; } = null!;

    [Export] public SecurityCodeForm FirstCodeForm { get; set; } = null!;

    [Export] public SecurityCodeForm ChangeCodeForm { get; set; } = null!;

    [Export] public DeleteCharacterForm DeleteForm { get; set; } = null!;

    [Export] public Toast Toast { get; set; } = null!;

    [Export] public MessagePopup MessagePopup { get; set; } = null!;

    [Signal]
    public delegate void EnterRequestedEventHandler();

    [Signal]
    public delegate void SelectionMoveRequestedEventHandler(int step);

    [Signal]
    public delegate void MapEnteredEventHandler();

    [Signal]
    public delegate void DeleteRequestedEventHandler();

    [Signal]
    public delegate void CharacterDeletedEventHandler(int characterIndex);

    [Signal]
    public delegate void NameplateFadeRequestedEventHandler(NameplateFade fade);

    public MapEnteredEvent? EnteredMap { get; private set; }

    public bool CanSelectCharacters => _state == State.Select;

    public bool IsDeleting => _state == State.Delete;

    protected override Control? NavigationStart => _state == State.Select ? PrimaryPill : FirstField;

    private SecurityCodeForm? CodeForm => _state switch
    {
        State.FirstCode => FirstCodeForm,
        State.ChangeCode => ChangeCodeForm,
        _ => null,
    };

    private Control? FirstField => _state == State.Delete ? DeleteForm.CodeEdit : CodeForm?.FirstField;

    private Control StateControl => _state switch
    {
        State.EnterWorld => EnterWorldLine,
        State.Delete => DeleteForm,
        _ => CodeForm ?? SelectActions,
    };

    public void Setup(IServerConnection connection, string accountName, ServerEntry? server, bool hasSecurityCode,
        string? entryFailureText)
    {
        _connection = connection;

        AccountLabel.Text = accountName;

        if (server != null)
        {
            ServerLine.ShowServer(server.Name, server.Address);
        }

        PrimaryPill.Pressed += PressPrimary;
        LogOutButton.Pressed += Leave;
        SecurityCodeButton.Pressed += OpenChangeCode;
        FirstCodeForm.CreateRequested += CreateSecurityCode;
        FirstCodeForm.LeaveRequested += Leave;
        FirstCodeForm.LinkAbove(LogOutButton);
        ChangeCodeForm.ChangeRequested += ChangeSecurityCode;
        ChangeCodeForm.LeaveRequested += CloseChangeCode;
        ChangeCodeForm.LinkAbove(LogOutButton);
        DeleteButton.Pressed += RequestDelete;
        DeleteForm.DeleteRequested += DeleteCharacter;
        DeleteForm.CancelRequested += CloseDelete;
        DeleteForm.LinkAbove(LogOutButton);

        Subscribe<DisconnectedEvent>(connection, CloseMessagePopup);
        Subscribe<MapEnteredEvent>(connection, CompleteBeginPlay);
        Subscribe<BeginPlayFailedEvent>(connection, ReportBeginPlayFailure);
        Subscribe<SecurityCodeCreatedEvent>(connection, CompleteSecurityCode);
        Subscribe<CreateSecurityCodeFailedEvent>(connection, ReportSecurityCodeFailure);
        Subscribe<SecurityCodeChangedEvent>(connection, CompleteSecurityCodeChange);
        Subscribe<ChangeSecurityCodeFailedEvent>(connection, ReportSecurityCodeChangeFailure);
        Subscribe<CharacterDeletedEvent>(connection, CompleteDelete);
        Subscribe<DeleteCharacterFailedEvent>(connection, ReportDeleteFailure);

        if (!hasSecurityCode)
        {
            ShowState(State.FirstCode);
        }

        if (entryFailureText != null)
        {
            MessagePopup.Open(EnterFailedTitle, entryFailureText);
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (MessagePopup.Visible || !inputEvent.IsActionPressed("ui_cancel"))
        {
            return;
        }

        if (_state == State.ChangeCode && !ChangeCodeForm.IsWaiting)
        {
            CloseChangeCode();
        }
        else if (_state == State.Delete && !DeleteForm.IsWaiting)
        {
            CloseDelete();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (_state != State.Select || GetViewport().GuiGetFocusOwner() != null || MessagePopup.Visible)
        {
            base._UnhandledInput(inputEvent);

            return;
        }

        if (inputEvent.IsActionPressed("ui_left"))
        {
            MoveSelection(-1);
        }
        else if (inputEvent.IsActionPressed("ui_right"))
        {
            MoveSelection(1);
        }
        else if (inputEvent.IsActionPressed("ui_accept"))
        {
            PressPrimary();

            GetViewport().SetInputAsHandled();
        }
        else
        {
            base._UnhandledInput(inputEvent);
        }
    }

    public void ShowActions(bool hasSelection, bool hasFreeSlot, bool hasCharacters)
    {
        _hasSelection = hasSelection;

        PrimaryPill.Text = hasSelection || !hasFreeSlot ? EnterWorldCaption : CreateCharacterCaption;
        PrimaryPill.Disabled = !hasSelection && !hasFreeSlot;

        CreateButton.Visible = hasSelection && hasFreeSlot;

        DeleteButton.Visible = hasCharacters;
        DeleteButton.Disabled = !hasSelection;

        LinkActions();
    }

    public void BeginPlay(int characterIndex, string characterName)
    {
        if (_state != State.Select)
        {
            return;
        }

        EnterWorldLabel.Text = $"Entering the world as {characterName}…";

        ShowState(State.EnterWorld);

        _connection.Send(new BeginPlayCommand(characterIndex));
    }

    public void OpenDelete(int characterIndex, CharacterSlot character)
    {
        if (_state != State.Select)
        {
            return;
        }

        _deletingIndex = characterIndex;
        _deletingName = character.Name;

        ShowState(State.Delete);

        DeleteForm.Open($"Delete {character.Name}?", $"Lv {character.Level} {character.Job} · can't be undone");
    }

    public void PlaceDimHole(Vector2 centre)
    {
        HoledDim.PlaceHole(centre);
    }

    private void MoveSelection(int step)
    {
        EmitSignal(SignalName.SelectionMoveRequested, step);

        GetViewport().SetInputAsHandled();
    }

    private void PressPrimary()
    {
        if (!_hasSelection || PrimaryPill.Disabled)
        {
            return;
        }

        EmitSignal(SignalName.EnterRequested);
    }

    private void LinkActions()
    {
        var row = new[] { PrimaryPill, CreateButton, SecurityCodeButton, DeleteButton }
            .Where(action => action.Visible)
            .Cast<Control>()
            .ToArray();

        for (var index = 0; index < row.Length - 1; index++)
        {
            LinkHorizontally(row[index], row[index + 1]);
        }

        row[0].FocusNeighborLeft = new NodePath();
        row[^1].FocusNeighborRight = new NodePath();

        foreach (var action in row)
        {
            LinkVertically(action, LogOutButton, null);
        }

        ChainFocus([.. row, LogOutButton]);

        LinkHeader();
    }

    private void LinkHeader()
    {
        LinkVertically(LogOutButton, null, FirstField ?? PrimaryPill);
    }

    private void CreateSecurityCode(string code)
    {
        FirstCodeForm.ShowWaiting(true);

        _connection.Send(new CreateSecurityCodeCommand(code));
    }

    private void CompleteSecurityCode(SecurityCodeCreatedEvent created)
    {
        if (_state != State.FirstCode)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        ShowState(State.Select);

        Toast.ShowMessage(SecurityCodeSetText);
    }

    private void ReportSecurityCodeFailure(CreateSecurityCodeFailedEvent failed)
    {
        if (_state != State.FirstCode)
        {
            return;
        }

        Log.Warning($"create security code failed with code {failed.Code}: {failed.Text}");

        FirstCodeForm.ShowWaiting(false);
        FirstCodeForm.ShowError(DescribeRefusal(failed.Code, failed.Text));
    }

    private void OpenChangeCode()
    {
        if (_state != State.Select)
        {
            return;
        }

        ShowState(State.ChangeCode);
    }

    private void CloseChangeCode()
    {
        if (_state != State.ChangeCode || ChangeCodeForm.IsWaiting)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        ShowState(State.Select);
    }

    private void ChangeSecurityCode(string currentCode, string newCode)
    {
        ChangeCodeForm.ShowWaiting(true);

        _connection.Send(new ChangeSecurityCodeCommand(currentCode, newCode));
    }

    private void CompleteSecurityCodeChange(SecurityCodeChangedEvent changed)
    {
        if (_state != State.ChangeCode)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        ShowState(State.Select);

        Toast.ShowMessage(SecurityCodeChangedText);
    }

    private void ReportSecurityCodeChangeFailure(ChangeSecurityCodeFailedEvent failed)
    {
        if (_state != State.ChangeCode)
        {
            return;
        }

        Log.Warning($"change security code failed with code {failed.Code}: {failed.Text}");

        ChangeCodeForm.ShowWaiting(false);

        if (failed.Code == WrongSecurityCode)
        {
            ChangeCodeForm.RejectCurrentCode(WrongCurrentCodeText);
        }
        else
        {
            ChangeCodeForm.ShowError(DescribeRefusal(failed.Code, failed.Text));
        }
    }

    private void RequestDelete()
    {
        if (_state != State.Select || !_hasSelection)
        {
            return;
        }

        EmitSignal(SignalName.DeleteRequested);
    }

    private void CloseDelete()
    {
        if (_state != State.Delete || DeleteForm.IsWaiting)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        ShowState(State.Select);
    }

    private void DeleteCharacter(string code)
    {
        DeleteForm.ShowWaiting(true);

        _connection.Send(new DeleteCharacterCommand(_deletingIndex, code));
    }

    private void CompleteDelete(CharacterDeletedEvent deleted)
    {
        if (_state != State.Delete)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        ShowState(State.Select);

        Toast.ShowMessage($"{_deletingName} deleted");

        EmitSignal(SignalName.CharacterDeleted, _deletingIndex);
    }

    private void ReportDeleteFailure(DeleteCharacterFailedEvent failed)
    {
        if (_state != State.Delete)
        {
            return;
        }

        Log.Warning($"delete character failed with code {failed.Code}: {failed.Text}");

        DeleteForm.ShowWaiting(false);

        if (failed.Code == WrongSecurityCode)
        {
            DeleteForm.RejectCode(WrongDeleteCodeText);
        }
        else
        {
            DeleteForm.ShowError(DescribeRefusal(failed.Code, failed.Text));
        }
    }

    private void CompleteBeginPlay(MapEnteredEvent entered)
    {
        if (_isLeaving)
        {
            return;
        }

        _connection.PauseDrain();

        EnteredMap = entered;

        EmitSignal(SignalName.MapEntered);
    }

    private void ReportBeginPlayFailure(BeginPlayFailedEvent failed)
    {
        ShowState(State.Select);

        Log.Warning($"begin play failed with code {failed.Code}: {failed.Text}");

        var text = failed.Text.Length > 0 ? failed.Text : "The server did not let the character enter.";

        MessagePopup.Open(EnterFailedTitle, text, failed.Code);
    }

    private void CloseMessagePopup(DisconnectedEvent disconnected)
    {
        MessagePopup.Hide();
    }

    private void ShowState(State state)
    {
        _state = state;

        ActionArea.Switch(StateControl);

        ShowSceneLook();
        LinkHeader();

        CodeForm?.Open();
    }

    private void ShowSceneLook()
    {
        var isDeleting = _state == State.Delete;
        var isDimmed = FirstField != null;

        _sceneLook?.Kill();
        _sceneLook = CreateTween().SetParallel();

        _sceneLook.TweenProperty(Dim, "modulate:a", isDimmed && !isDeleting ? 1f : 0f, SceneLookSeconds);
        _sceneLook.TweenProperty(HoledDim, "modulate:a", isDeleting ? 1f : 0f, SceneLookSeconds);
        _sceneLook.TweenProperty(BottomFade, "offset_top", -(isDimmed ? FormFadeHeight : SelectFadeHeight),
            SceneLookSeconds);

        var fade = isDeleting ? NameplateFade.AllButSelected : isDimmed ? NameplateFade.All : NameplateFade.None;

        EmitSignal(SignalName.NameplateFadeRequested, Variant.From(fade));
    }

    private void Leave()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;

        Close();
    }

    private static string DescribeRefusal(int code, string text)
    {
        if (code is GuildLeader or ServerBusy or CharacterOnline)
        {
            return text;
        }

        return text.Length > 0 ? $"{text} · Error {code}" : $"Error {code}";
    }

    private enum State
    {
        Select,
        EnterWorld,
        FirstCode,
        ChangeCode,
        Delete,
    }
}
