using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Camera;
using Top.Client.Features.Characters;
using Top.Client.Features.Characters.Movement;
using Top.Client.Features.Hud;
using Top.Client.Features.Inventory;
using Top.Client.Features.Inventory.Bag;
using Top.Client.Features.Inventory.Tooltip;
using Top.Client.Features.Lobby.Select;
using Top.Client.Features.Maps;
using Top.Client.Features.Party;
using Top.Client.Messages;
using Top.Client.Messages.Inventory.Commands;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Inventory.Events;
using Top.Client.Messages.Party.Commands;
using Top.Client.Messages.Party.Events;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Client.Messages.Play.Events;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.World;
using Top.Logging;

namespace Top.Client.Features.World;

public partial class WorldScene : Node
{
    private static readonly StringName InventoryAction = "inventory";

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly List<IDisposable> _subscriptions = [];

    private readonly InputGate _inputGate = new InputGate();

    private readonly PartyInviteQueue _partyInviteQueue = new();

    private PartyRoster _party = null!;

    private CharacterInventory _inventory = null!;

    private ItemTable _itemTable = null!;

    private Session _session = null!;

    private IServerConnection _connection = null!;

    private ServerContent _content = null!;

    private SceneRouter _router = null!;

    private WorldPayload _payload = null!;

    private FloatingOrigin _floatingOrigin = null!;

    private CharacterRegistry _characterRegistry = null!;

    private Character? _player;

    private PredictedMotion? _predictedMotion;

    private string? _entryFailureText;

    [Export] public Node3D WorldRoot { get; set; } = null!;

    [Export] public Map Map { get; set; } = null!;

    [Export] public FollowCamera Camera { get; set; } = null!;

    [Export] public Node3D Characters { get; set; } = null!;

    [Export] public PlayerController PlayerController { get; set; } = null!;

    [Export] public PackedScene NameplateScene { get; set; } = null!;

    [Export] public HudScreen Hud { get; set; } = null!;

    [Export] public LoadingScreen LoadingScreen { get; set; } = null!;

    [Export] public GameMenu GameMenu { get; set; } = null!;

    public void Setup(Session session, IServerConnection connection, ServerContent content,
        SceneRouter router, WorldPayload payload)
    {
        _session = session;
        _connection = connection;
        _content = content;
        _router = router;
        _payload = payload;
        _party = new PartyRoster(payload.EnteredMap.Character.WorldId);
        _inventory = new CharacterInventory(payload.EnteredMap);
    }

    public override void _Ready()
    {
        _subscriptions.Add(_connection.Subscribe<PlayEndedEvent>(ReturnToCharacterSelect));
        _subscriptions.Add(_connection.Subscribe<EndPlayFailedEvent>(ReportEndPlayFailure));
        _subscriptions.Add(_connection.Subscribe<CharacterAppearedEvent>(AddSeenCharacter));
        _subscriptions.Add(_connection.Subscribe<CharacterDisappearedEvent>(RemoveCharacter));
        _subscriptions.Add(_connection.Subscribe<CharacterMovedEvent>(FollowCharacter));
        _subscriptions.Add(_connection.Subscribe<ActionFailedEvent>(RecoverFromFailedAction));
        _subscriptions.Add(_connection.Subscribe<PreMoveTimeChangedEvent>(ChangePreMoveTime));
        _subscriptions.Add(_connection.Subscribe<CharacterAttributesChangedEvent>(ChangeAttributes));
        _subscriptions.Add(_connection.Subscribe<CharacterLookChangedEvent>(ChangeLook));
        _subscriptions.Add(_connection.Subscribe<FairyConditionChangedEvent>(ChangeFairyCondition));
        _subscriptions.Add(_connection.Subscribe<ShipLookChangedEvent>(ChangeShipLook));
        _subscriptions.Add(_connection.Subscribe<PartyInvitedEvent>(HandleInvited));
        _subscriptions.Add(_connection.Subscribe<PartyInviteEndedEvent>(HandleInviteEnded));
        _subscriptions.Add(_connection.Subscribe<PartyChangedEvent>(ChangeParty));
        _subscriptions.Add(_connection.Subscribe<PartyMemberVitalsChangedEvent>(ChangePartyMemberVitals));
        _subscriptions.Add(_connection.Subscribe<CharacterTeamLeaderChangedEvent>(ChangeTeamLeader));
        _subscriptions.Add(_connection.Subscribe<BagChangedEvent>(ChangeBag));
        _subscriptions.Add(_connection.Subscribe<TemporaryBagChangedEvent>(ChangeTemporaryBag));
        _subscriptions.Add(_connection.Subscribe<BagLockReportedEvent>(ChangeBagLock));
        _subscriptions.Add(_connection.Subscribe<ItemActionFailedEvent>(ReportItemActionFailure));

        _floatingOrigin = new FloatingOrigin(WorldRoot);

        GameMenu.SwitchCharacterConfirmed += SwitchCharacter;
        GameMenu.LogOutConfirmed += LogOut;
        GameMenu.ExitConfirmed += Exit;

        Hud.InviteAccepted += AcceptInvite;
        Hud.InviteDeclined += DeclineInvite;
        Hud.BagLockRequested += LockBag;
        Hud.BagUnlockRequested += UnlockBag;
        Hud.BagItemMoveRequested += MoveBagItem;
        Hud.BagItemSplitRequested += SplitBagItem;
        Hud.BagSortRequested += SortBag;
        Hud.BagItemUseRequested += UseBagItem;
        Hud.BagItemEquipRequested += EquipBagItem;
        Hud.BagItemEquipToSlotRequested += EquipBagItemToSlot;
        Hud.ItemUnequipRequested += UnequipItem;
        Hud.TemporaryBagItemTakeRequested += TakeTemporaryBagItem;
        Hud.TemporaryBagItemSplitRequested += SplitTemporaryBagItem;
        Hud.TemporaryBagTakeAllRequested += TakeAllTemporaryBagItems;
        Hud.BagItemDeleteRequested += DeleteBagItem;
        Hud.BagItemThrowRequested += ThrowBagItem;

        LoadingScreen.VisibilityChanged += UpdateHudVisibility;

        Map.Setup(_content);

        _connection.Send(new SyncTemporaryBagCommand());

        EnterMap();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (LoadingScreen.Visible || GameMenu.IsLocked)
        {
            return;
        }

        if (inputEvent.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();

            StepBack();
        }
        else if (inputEvent.IsActionPressed(InventoryAction) && !GameMenu.IsOpen)
        {
            GetViewport().SetInputAsHandled();

            Hud.ToggleInventory();
        }
    }

    public override void _Process(double delta)
    {
        if (IsStandingInWorld())
        {
            _predictedMotion.Process(delta);
            _floatingOrigin.Recentre(_player.Position);

            Map.SetStreamingCenter(_player.Position);
        }
    }

    public override void _ExitTree()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            ReleaseSubscriptions();
        }
    }

    private static string ToEntryFailureText(string mapName, Exception exception)
    {
        return exception switch
        {
            MapNotFoundException =>
                $"The server sent your character to {mapName}, a map this client does not know.",
            MapFileNotFoundException missing =>
                $"The map {missing.Map.DisplayName} is not installed. This client's content is incomplete.",
            _ => $"Your character could not enter {mapName}."
        };
    }

    private async void EnterMap()
    {
        var entered = _payload.EnteredMap;

        if (entered.EnterType == EnterType.Carry)
        {
            LoadingScreen.Begin();
        }
        else
        {
            LoadingScreen.Hide();
        }

        try
        {
            await LoadMap(entered, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Log.Error($"map {entered.MapName} broke while loading", exception);

            LeaveWorld(ToEntryFailureText(entered.MapName, exception));
        }
        finally
        {
            _connection.ResumeDrain();
        }
    }

    private async Task LoadMap(MapEnteredEvent entered, CancellationToken cancellationToken)
    {
        var tipTable = await _content.TableReader.ReadTips(cancellationToken);

        if (tipTable.Count > 0)
        {
            LoadingScreen.ShowTip(PickTip(tipTable));
        }

        var characterTable = await _content.TableReader.ReadCharacters(cancellationToken);
        var rigTable = await _content.TableReader.ReadRigs(cancellationToken);
        var actionSetTable = await _content.TableReader.ReadActionSets(cancellationToken);
        var itemTable = await _content.TableReader.ReadItems(cancellationToken);
        _itemTable = itemTable;
        var shipTable = await _content.TableReader.ReadShips(cancellationToken);
        var shipPartTable = await _content.TableReader.ReadShipParts(cancellationToken);
        var areaTable = await _content.TableReader.ReadAreas(cancellationToken);

        var jobTable = await _content.TableReader.ReadJobs(cancellationToken);
        var portraitCameraTable = await _content.TableReader.ReadPortraitCameras(cancellationToken);
        var itemDescriber = new ItemDescriber(await _content.TableReader.ReadItemTypes(cancellationToken),
            await _content.TableReader.ReadItemPrefixes(cancellationToken),
            await _content.TableReader.ReadGems(cancellationToken), jobTable);

        var spawnPoint = new Vector2(entered.Character.Position.X, entered.Character.Position.Y);

        await Map.Load(lookup => lookup.FindByName(entered.MapName), LoadingScreen.ShowStage,
            (mapEntry, mapGrid) =>
                LoadingScreen.ShowDestination(new DestinationLookup(mapEntry, areaTable, mapGrid).FindAt(spawnPoint)),
            cancellationToken);

        _characterRegistry = new CharacterRegistry(Characters, NameplateScene, Camera, _inputGate, characterTable,
            _content.ModelFactory, rigTable, actionSetTable, itemTable, shipTable, shipPartTable, Map.Grid);

        var player = _characterRegistry.Add(entered.Character, entered.Character);

        Map.SetStreamingCenter(player.Position);

        var walkability = new Walkability(Map.Grid, player.State.ControlType, (Territory)player.Entry.Territory);

        _predictedMotion = new PredictedMotion(player, _connection, new Pathfinder(walkability));
        _player = player;

        Camera.Follow(player, _inputGate);

        PlayerController.Setup(Camera, WorldRoot, Map.Grid, _predictedMotion, _inputGate);

        Hud.Setup(jobTable, itemTable, itemDescriber, _content.TextureCache, characterTable, portraitCameraTable,
            _content.ModelFactory, rigTable, actionSetTable);
        Hud.SetPlayer(player.State);
        Hud.ShowInventory(_inventory);

        UpdateHudVisibility();

        LoadingScreen.Finish();

        Log.Info($"entered {Map.Entry.DisplayName} ({Map.Entry.Name}) at ({entered.Character.Position.X} m, "
                 + $"{entered.Character.Position.Y} m) facing {entered.Character.Angle} as {entered.Character.Name}, "
                 + $"standing at {player.Position}");
    }

    private static string PickTip(TipTable tips)
    {
        return tips.ElementAt(Random.Shared.Next(tips.Count)).Text;
    }

    private void AddSeenCharacter(CharacterAppearedEvent appeared)
    {
        var worldId = appeared.Character.WorldId;

        if (!IsStandingInWorld() || IsOwnCharacter(worldId))
        {
            return;
        }

        _characterRegistry.Remove(worldId);
        _characterRegistry.Add(appeared.Character, _player.State);
    }

    private void RemoveCharacter(CharacterDisappearedEvent disappeared)
    {
        if (IsStandingInWorld() && !IsOwnCharacter(disappeared.WorldId))
        {
            _characterRegistry.Remove(disappeared.WorldId);
        }
    }

    private void FollowCharacter(CharacterMovedEvent moved)
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        if (IsOwnCharacter(moved.WorldId))
        {
            _predictedMotion.Correct(moved.Points[^1], moved.MoveState);
        }
        else
        {
            _characterRegistry.Follow(moved.WorldId, moved.Points, moved.MoveState);
        }
    }

    private void RecoverFromFailedAction(ActionFailedEvent failed)
    {
        if (IsStandingInWorld() && IsOwnCharacter(failed.WorldId))
        {
            _predictedMotion.RecoverFromFailedAction(failed.Reason);
        }
    }

    private void ChangePreMoveTime(PreMoveTimeChangedEvent changed)
    {
        if (IsStandingInWorld())
        {
            _predictedMotion.PreMoveTime = (float)TimeSpan.FromMilliseconds(changed.Milliseconds).TotalSeconds;
        }
    }

    private void ChangeAttributes(CharacterAttributesChangedEvent changed)
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        var character = _characterRegistry.Find(changed.WorldId);

        character?.ChangeAttributes(changed.Attributes);

        if (IsOwnCharacter(changed.WorldId))
        {
            Hud.SetPlayer(_player.State);
        }

        if (changed.Attributes.MoveSpeed is not { } moveSpeed)
        {
            return;
        }

        if (IsOwnCharacter(changed.WorldId))
        {
            _predictedMotion.ChangeSpeed(moveSpeed);
        }
        else
        {
            character?.Motion.ChangeSpeed(moveSpeed);
        }
    }

    private void ChangeLook(CharacterLookChangedEvent changed)
    {
        _inventory.ChangeEquipment(changed);
        RefreshInventory();

        if (!IsStandingInWorld())
        {
            return;
        }

        _characterRegistry.Find(changed.WorldId)?.ChangeLook(changed.Look);

        if (IsOwnCharacter(changed.WorldId))
        {
            Hud.SetPlayer(_player.State);
        }
    }

    private void ChangeFairyCondition(FairyConditionChangedEvent changed)
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        _characterRegistry.Find(changed.WorldId)?.ChangeFairyCondition(changed.ItemId, changed.Stamina, changed.Growth);

        if (IsOwnCharacter(changed.WorldId))
        {
            Hud.SetPlayer(_player.State);
        }
    }

    private void ChangeShipLook(ShipLookChangedEvent changed)
    {
        if (IsStandingInWorld())
        {
            _characterRegistry.Find(changed.WorldId)?.ChangeShipLook(changed.Look);
        }
    }

    private void ChangeTeamLeader(CharacterTeamLeaderChangedEvent changed)
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        _characterRegistry.Find(changed.WorldId)?.ChangeTeamLeader(changed.TeamLeaderId);

        if (IsOwnCharacter(changed.WorldId))
        {
            _characterRegistry.ChangePlayer(_player.State);
        }
    }

    private void ChangeParty(PartyChangedEvent changed)
    {
        _party.ChangeMembers(changed);
        RefreshParty();
    }

    private void ChangePartyMemberVitals(PartyMemberVitalsChangedEvent changed)
    {
        _party.ChangeVitals(changed);
        RefreshParty();
    }

    private void RefreshParty()
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        Hud.ShowParty(_party.Members, _party.IsPlayerLeader);
    }

    private void ChangeBag(BagChangedEvent changed)
    {
        if (_inventory.ChangeBag(changed) is { } nextSortMove)
        {
            _connection.Send(nextSortMove);
        }

        RefreshInventory();
    }

    private void ChangeTemporaryBag(TemporaryBagChangedEvent changed)
    {
        _inventory.ChangeTemporaryBag(changed);
        RefreshInventory();
    }

    private void ChangeBagLock(BagLockReportedEvent reported)
    {
        var result = _inventory.ChangeBagLock(reported);

        RefreshInventory();

        if (result == BagLockResult.CodeRejected)
        {
            Hud.RejectBagUnlockCode();
        }
    }

    private void LockBag()
    {
        _connection.Send(_inventory.LockBag());
    }

    private void UnlockBag(string code)
    {
        _connection.Send(_inventory.UnlockBag(code));
        RefreshInventory();
    }

    private void MoveBagItem(int source, int destination)
    {
        if (_inventory.MoveBagItem(source, destination) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void SplitBagItem(int source, int count, int destination)
    {
        if (_inventory.SplitBagItem(source, count, destination) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void SortBag(int key)
    {
        if (_inventory.SortBag((BagSortKey)key, _itemTable) is { } command)
        {
            _connection.Send(command);
            RefreshInventory();
        }
    }

    private void UseBagItem(int source)
    {
        if (_inventory.UseBagItem(source, _itemTable, DateTime.UtcNow) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void EquipBagItem(int source)
    {
        if (_inventory.EquipBagItem(source, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void EquipBagItemToSlot(int source, int slot)
    {
        if (_inventory.EquipBagItem(source, (EquipmentSlot)slot, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void UnequipItem(int slot, int destination)
    {
        if (_inventory.UnequipItem((EquipmentSlot)slot, destination) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void TakeTemporaryBagItem(int source, int destination)
    {
        if (_inventory.TakeTemporaryBagItem(source, destination, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void SplitTemporaryBagItem(int source, int count, int destination)
    {
        if (_inventory.SplitTemporaryBagItem(source, count, destination, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void TakeAllTemporaryBagItems()
    {
        foreach (var command in _inventory.TakeAllTemporaryBagItems())
        {
            _connection.Send(command);
        }
    }

    private void DeleteBagItem(int source)
    {
        if (_inventory.DeleteBagItem(source, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void ThrowBagItem(int source, int count)
    {
        if (!IsStandingInWorld())
        {
            return;
        }

        var dropPosition = new System.Numerics.Vector2(_player.Motion.MapPosition.X, _player.Motion.MapPosition.Y);

        if (_inventory.ThrowBagItem(source, count, dropPosition, _itemTable) is { } command)
        {
            _connection.Send(command);
        }
    }

    private void ReportItemActionFailure(ItemActionFailedEvent failed)
    {
        _inventory.FailItemAction();
        Hud.ShowItemActionFailure(failed.Failure);
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        if (IsStandingInWorld())
        {
            Hud.ShowInventory(_inventory);
        }
    }

    private void HandleInvited(PartyInvitedEvent invited)
    {
        _partyInviteQueue.AddInvite(invited.InviterId, invited.InviterName, DateTime.UtcNow);
        RefreshInvites();
    }

    private void HandleInviteEnded(PartyInviteEndedEvent ended)
    {
        _partyInviteQueue.RemoveInvite(ended.InviterId);
        RefreshInvites();
    }

    private void RefreshInvites()
    {
        Hud.ShowInvites(_partyInviteQueue.Invites);
    }

    private void AcceptInvite(uint inviterId)
    {
        _connection.Send(new AcceptInviteCommand(inviterId));
        _partyInviteQueue.RemoveInvite(inviterId);
        RefreshInvites();
    }

    private void DeclineInvite(uint inviterId)
    {
        _connection.Send(new DeclineInviteCommand(inviterId));
        _partyInviteQueue.RemoveInvite(inviterId);
        RefreshInvites();
    }

    [MemberNotNullWhen(true, nameof(_player), nameof(_predictedMotion))]
    private bool IsStandingInWorld()
    {
        return _player != null && _predictedMotion != null;
    }

    private void UpdateHudVisibility()
    {
        Hud.Visible = IsStandingInWorld() && !LoadingScreen.Visible;
    }

    private bool IsOwnCharacter(uint worldId)
    {
        return worldId == _player!.State.WorldId;
    }

    private void StepBack()
    {
        // TODO: the top open window closes first, once the world has more than one window.
        if (GameMenu.IsConfirming)
        {
            GameMenu.CancelConfirm();
        }
        else if (GameMenu.IsOpen)
        {
            GameMenu.Dismiss();
        }
        else if (Hud.IsInventoryOpen)
        {
            Hud.CloseInventory();
        }
        else
        {
            GameMenu.Open();
        }

        _inputGate.IsBlocked = GameMenu.IsOpen;
    }

    private void LogOut()
    {
        _router.LeaveServer();
    }

    private void Exit()
    {
        var tree = GetTree();

        _router.LeaveServer();

        tree.Quit();
    }

    private void SwitchCharacter()
    {
        LoadingScreen.Return();

        EndPlay();
    }

    private void LeaveWorld(string entryFailureText)
    {
        _entryFailureText = entryFailureText;

        LoadingScreen.Fail();

        EndPlay();
    }

    private void EndPlay()
    {
        _inputGate.IsBlocked = true;

        Map.Clear();

        _connection.Send(new EndPlayCommand());
    }

    private void ReturnToCharacterSelect(PlayEndedEvent ended)
    {
        _router.GoToCharacterSelect(
            new CharacterSelectPayload(ended.Characters, ended.SlotCap, HasSecurityCode: true, _entryFailureText));
    }

    private void ReportEndPlayFailure(EndPlayFailedEvent failed)
    {
        Log.Warning($"end play failed with code {failed.Code}: {failed.Text}");

        var refusal = failed.Text.Length > 0 ? failed.Text : failed.Code.ToString();

        _router.LeaveServer(_entryFailureText == null ? refusal : $"{_entryFailureText}\n{refusal}");
    }

    private void ReleaseSubscriptions()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }
}
