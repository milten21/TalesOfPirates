using System;
using Godot;
using Top.Client.Features.Characters.Movement;
using Top.Client.Features.Maps;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Contracts.Tables.Ships;
using Top.Logging;

namespace Top.Client.Features.Characters;

public partial class Character : Node3D
{
    private const string BodyName = "Body";

    private const int WaitingAction = 1;

    private const int RunAction = 5;

    private const int DeathAction = 17;

    private CharacterBody _body = null!;

    private Nameplate _nameplate = null!;

    private ShipTable _shipTable = null!;

    private ShipPartTable _shipPartTable = null!;

    [Signal]
    public delegate void ActionEventEventHandler(int action, string name);

    public CharacterState State { get; private set; } = null!;

    public CharacterEntry Entry { get; private set; } = null!;

    public CharacterMotion Motion { get; private set; } = null!;

    public float HeightOffset { get; set; }

    public static Character Create(CharacterState state, CharacterEntry entry, ModelFactory modelFactory,
        RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable, ShipTable shipTable,
        ShipPartTable shipPartTable, MapGrid mapGrid, Nameplate nameplate)
    {
        var character = new Character
        {
            Name = state.WorldId.ToString(),
            State = state,
            Entry = entry,
            _nameplate = nameplate,
            _shipTable = shipTable,
            _shipPartTable = shipPartTable,
        };

        character._body = character.LoadBody(modelFactory, rigTable, actionSetTable, itemTable);
        character._body.Root.Name = BodyName;
        character._body.ActionEvent += (action, name) =>
            character.EmitSignal(SignalName.ActionEvent, action, name);
        character.AddChild(character._body.Root);

        nameplate.Position = Vector3.Up * entry.Height;
        character.AddChild(nameplate);

        character.Motion = CharacterMotion.Create(character, mapGrid);
        character.AddChild(character.Motion);

        return character;
    }

    public override void _Ready()
    {
        _body.PreloadActions(WaitingAction, RunAction, DeathAction);
        _body.PlayAction(WaitingAction, PlayMode.Loop);
    }

    public void PlayAction(int action, PlayMode mode, float blend = 0f, float speed = 1f)
    {
        _body.PlayAction(action, mode, blend, speed);
    }

    public void ChangeLook(CharacterLook look)
    {
        State = State with { Look = look };

        _body.ChangeLook(look);
    }

    public void ChangeFairyCondition(int itemId, int stamina, int growth)
    {
        if (State.Look is not { Fairy: { } fairy } look || fairy.ItemId != itemId)
        {
            return;
        }

        var changedFairy = fairy with { Stamina = stamina, Growth = growth };

        State = State with { Look = new CharacterLook(look.HairId, look.EquipmentIds, changedFairy) };
    }

    public void ChangeAttributes(ChangedAttributes changed)
    {
        State = State with
        {
            Level = changed.Level ?? State.Level,
            Health = changed.Health ?? State.Health,
            MaxHealth = changed.MaxHealth ?? State.MaxHealth,
            Mana = changed.Mana ?? State.Mana,
            MaxMana = changed.MaxMana ?? State.MaxMana,
            Job = changed.Job ?? State.Job,
            Experience = changed.Experience ?? State.Experience,
            LevelThreshold = changed.LevelThreshold ?? State.LevelThreshold,
            NextLevelThreshold = changed.NextLevelThreshold ?? State.NextLevelThreshold,
            Gold = changed.Gold ?? State.Gold,
            MoveSpeed = changed.MoveSpeed ?? State.MoveSpeed,
        };

        _nameplate.ChangeCharacter(State);
    }

    public void ChangeTeamLeader(uint teamLeaderId)
    {
        State = State with { TeamLeaderId = teamLeaderId };

        _nameplate.ChangeCharacter(State);
    }

    public void ChangePlayer(CharacterState player)
    {
        _nameplate.ChangePlayer(player);
    }

    public void ChangeShipLook(ShipLook look)
    {
        if (Entry.ModalType != CharacterModalType.Boat)
        {
            Log.Info($"{State.Name} (type {Entry.Id}) is not a boat and shows no ship {look.ShipId}");

            return;
        }

        _body.ShowParts(SelectShipParts(look));
    }

    private CharacterBody LoadBody(ModelFactory modelFactory, RigTable rigTable, ActionSetTable actionSetTable,
        ItemTable itemTable)
    {
        return Entry.ModalType == CharacterModalType.Boat && State.ShipLook is { } shipLook
            ? CharacterBody.LoadParts(Entry, SelectShipParts(shipLook), modelFactory, actionSetTable)
            : CharacterBody.Load(Entry, State.Look, modelFactory, rigTable, actionSetTable, itemTable);
    }

    private string?[] SelectShipParts(ShipLook look)
    {
        var parts = new string?[BoatSlots.Count];

        if (_shipTable.TryGetById(look.ShipId, out var ship))
        {
            parts[BoatSlots.Hull] = FindShipPartPath(ship.HullId);
        }
        else
        {
            Log.Warning($"{State.Name} (type {Entry.Id}) is ship {look.ShipId}, which the ship table does not carry");
        }

        parts[BoatSlots.Bow] = FindShipPartPath(look.BowId);

        if (_shipPartTable.TryGetById(look.EngineId, out var engine))
        {
            parts[BoatSlots.Engine] = engine.ModelPath;

            for (var propeller = 0; propeller < Math.Min(engine.PropellerIds.Length, BoatSlots.PropellerCount); propeller++)
            {
                parts[BoatSlots.FirstPropeller + propeller] = FindShipPartPath(engine.PropellerIds[propeller]);
            }
        }

        return parts;
    }

    private string? FindShipPartPath(int partId)
    {
        return _shipPartTable.TryGetById(partId, out var part) ? part.ModelPath : null;
    }
}
