using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Features.Inventory;
using Top.Client.Features.Inventory.Bag;
using Top.Client.Features.Inventory.Tooltip;
using Top.Client.Features.Party;
using Top.Client.Messages.Inventory.Data;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Client.Rendering.Textures;
using Top.Client.UI;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;

namespace Top.Client.Features.Hud;

public partial class HudScreen : Screen
{
    private const float StackSeparation = 16f;

    private const float FairyPlateMarginBottom = 4f;

    private readonly Dictionary<uint, PartyRow> _partyRows = [];

    private JobTable _jobTable = null!;

    private CharacterTable _characterTable = null!;

    private PortraitCameraTable _portraitCameraTable = null!;

    private ModelFactory _modelFactory = null!;

    private RigTable _rigTable = null!;

    private ActionSetTable _actionSetTable = null!;

    private ItemTable _itemTable = null!;

    [Signal] public delegate void InviteAcceptedEventHandler(uint inviterId);

    [Signal] public delegate void InviteDeclinedEventHandler(uint inviterId);

    [Signal] public delegate void BagLockRequestedEventHandler();

    [Signal] public delegate void BagUnlockRequestedEventHandler(string code);

    [Signal] public delegate void BagItemMoveRequestedEventHandler(int source, int destination);

    [Signal] public delegate void BagItemSplitRequestedEventHandler(int source, int count, int destination);

    [Signal] public delegate void BagSortRequestedEventHandler(int key);

    [Signal] public delegate void BagItemUseRequestedEventHandler(int source);

    [Signal] public delegate void BagItemEquipRequestedEventHandler(int source);

    [Signal] public delegate void BagItemEquipToSlotRequestedEventHandler(int source, int slot);

    [Signal] public delegate void ItemUnequipRequestedEventHandler(int slot, int destination);

    [Signal] public delegate void TemporaryBagItemTakeRequestedEventHandler(int source, int destination);

    [Signal] public delegate void TemporaryBagItemSplitRequestedEventHandler(int source, int count, int destination);

    [Signal] public delegate void TemporaryBagTakeAllRequestedEventHandler();

    [Signal] public delegate void BagItemDeleteRequestedEventHandler(int source);

    [Signal] public delegate void BagItemThrowRequestedEventHandler(int source, int count);

    [Export] public PlayerFrame PlayerFrame { get; set; } = null!;

    [Export] public FairyPlate FairyPlate { get; set; } = null!;

    [Export] public Control Stack { get; set; } = null!;

    [Export] public Control PartyBlock { get; set; } = null!;

    [Export] public Container PartyRows { get; set; } = null!;

    [Export] public PackedScene PartyRowScene { get; set; } = null!;

    [Export] public InviteCard InviteCard0 { get; set; } = null!;

    [Export] public InviteCard InviteCard1 { get; set; } = null!;

    [Export] public InventoryWindow InventoryWindow { get; set; } = null!;

    [Export] public Toast Toast { get; set; } = null!;

    public bool IsInventoryOpen => InventoryWindow.Visible;

    public void Setup(JobTable jobs, ItemTable items, ItemDescriber itemDescriber, TextureCache textures,
        CharacterTable characters, PortraitCameraTable portraitCameras, ModelFactory modelFactory, RigTable rigs, ActionSetTable actionSets)
    {
        _jobTable = jobs;
        _characterTable = characters;
        _portraitCameraTable = portraitCameras;
        _modelFactory = modelFactory;
        _rigTable = rigs;
        _actionSetTable = actionSets;
        _itemTable = items;

        PlayerFrame.Setup(characters, portraitCameras, modelFactory, rigs, actionSets, items);
        FairyPlate.Setup(items, textures);
        InventoryWindow.Setup(items, itemDescriber, textures);
    }

    public override void _Ready()
    {
        ConnectCard(InviteCard0);
        ConnectCard(InviteCard1);

        InventoryWindow.BagLockRequested += () => EmitSignal(SignalName.BagLockRequested);
        InventoryWindow.BagUnlockRequested += code => EmitSignal(SignalName.BagUnlockRequested, code);
        InventoryWindow.UnlockCodeRejected += error => Toast.ShowMessage(error);
        InventoryWindow.BagItemMoveRequested += (source, destination) =>
            EmitSignal(SignalName.BagItemMoveRequested, source, destination);
        InventoryWindow.BagItemSplitRequested += (source, count, destination) =>
            EmitSignal(SignalName.BagItemSplitRequested, source, count, destination);
        InventoryWindow.BagSortRequested += key => EmitSignal(SignalName.BagSortRequested, key);
        InventoryWindow.BagItemUseRequested += source => EmitSignal(SignalName.BagItemUseRequested, source);
        InventoryWindow.BagItemEquipRequested += source => EmitSignal(SignalName.BagItemEquipRequested, source);
        InventoryWindow.BagItemEquipToSlotRequested += (source, slot) =>
            EmitSignal(SignalName.BagItemEquipToSlotRequested, source, slot);
        InventoryWindow.ItemUnequipRequested += (slot, destination) =>
            EmitSignal(SignalName.ItemUnequipRequested, slot, destination);
        InventoryWindow.TemporaryBagItemTakeRequested += (source, destination) =>
            EmitSignal(SignalName.TemporaryBagItemTakeRequested, source, destination);
        InventoryWindow.TemporaryBagItemSplitRequested += (source, count, destination) =>
            EmitSignal(SignalName.TemporaryBagItemSplitRequested, source, count, destination);
        InventoryWindow.TemporaryBagTakeAllRequested += () => EmitSignal(SignalName.TemporaryBagTakeAllRequested);
        InventoryWindow.BagItemDeleteRequested += source => EmitSignal(SignalName.BagItemDeleteRequested, source);
        InventoryWindow.BagItemThrowRequested += (source, count) =>
            EmitSignal(SignalName.BagItemThrowRequested, source, count);
    }

    public void SetPlayer(CharacterState player)
    {
        PlayerFrame.ShowCharacter(player, _jobTable);
        FairyPlate.ShowFairy(player.Look?.Fairy);
        InventoryWindow.ShowPlayer(player);

        Stack.Position = new Vector2(Stack.Position.X, FairyPlate.Visible
            ? FairyPlate.Position.Y + FairyPlate.Size.Y + FairyPlateMarginBottom + StackSeparation
            : PlayerFrame.Position.Y + PlayerFrame.Size.Y + StackSeparation);
    }

    public void ShowParty(IReadOnlyList<PartyRosterMember> members, bool isPlayerLeader)
    {
        PlayerFrame.ShowLeader(isPlayerLeader);

        RemoveLeftRows(members);

        for (var index = 0; index < members.Count; index++)
        {
            var row = FindOrAddRow(members[index].CharacterId);

            PartyRows.MoveChild(row, index);
            row.ShowMember(members[index]);
        }

        PartyBlock.Visible = members.Count > 0;
    }

    public void ShowInventory(CharacterInventory inventory)
    {
        InventoryWindow.ShowInventory(inventory);
    }

    public void RejectBagUnlockCode()
    {
        InventoryWindow.RejectUnlockCode();
    }

    public void ShowItemActionFailure(ItemActionFailure failure)
    {
        Toast.ShowMessage(ItemActionFailureText.Describe(failure));
    }

    public void ToggleInventory()
    {
        InventoryWindow.Visible = !InventoryWindow.Visible;
    }

    public void CloseInventory()
    {
        InventoryWindow.Hide();
    }

    public void ShowInvites(IReadOnlyList<PendingInvite> invites)
    {
        ShowCard(InviteCard0, invites.Count > 0 ? invites[0] : null);
        ShowCard(InviteCard1, invites.Count > 1 ? invites[1] : null);
    }

    private void RemoveLeftRows(IReadOnlyList<PartyRosterMember> members)
    {
        var leftIds = _partyRows.Keys
            .Where(characterId => members.All(member => member.CharacterId != characterId))
            .ToList();

        foreach (var characterId in leftIds)
        {
            var row = _partyRows[characterId];

            _partyRows.Remove(characterId);
            PartyRows.RemoveChild(row);
            row.QueueFree();
        }
    }

    private PartyRow FindOrAddRow(uint characterId)
    {
        if (_partyRows.TryGetValue(characterId, out var row))
        {
            return row;
        }

        row = PartyRowScene.Instantiate<PartyRow>();
        row.Setup(_characterTable, _portraitCameraTable, _modelFactory, _rigTable, _actionSetTable, _itemTable);

        PartyRows.AddChild(row);
        _partyRows[characterId] = row;

        return row;
    }

    private void ConnectCard(InviteCard card)
    {
        card.Accepted += inviterId => EmitSignal(SignalName.InviteAccepted, inviterId);
        card.Declined += inviterId => EmitSignal(SignalName.InviteDeclined, inviterId);
    }

    private static void ShowCard(InviteCard card, PendingInvite? invite)
    {
        card.Visible = invite != null;

        if (invite != null)
        {
            card.ShowInvite(invite);
        }
    }
}
