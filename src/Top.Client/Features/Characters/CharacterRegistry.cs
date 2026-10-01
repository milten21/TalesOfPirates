using System.Collections.Generic;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Characters.Movement;
using Top.Client.Features.Maps;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Contracts.Tables.Ships;

namespace Top.Client.Features.Characters;

public class CharacterRegistry(
    Node3D characterRoot,
    PackedScene nameplateScene,
    Camera3D camera,
    InputGate inputGate,
    CharacterTable characterTable,
    ModelFactory modelFactory,
    RigTable rigTable,
    ActionSetTable actionSetTable,
    ItemTable itemTable,
    ShipTable shipTable,
    ShipPartTable shipPartTable,
    MapGrid mapGrid)
{
    private readonly Dictionary<uint, Character> _characters = new Dictionary<uint, Character>();

    private readonly Dictionary<uint, FollowedMotion> _followedMotions = new Dictionary<uint, FollowedMotion>();

    public Character Add(CharacterState state, CharacterState player)
    {
        var nameplate = nameplateScene.Instantiate<Nameplate>();

        nameplate.Setup(camera, inputGate, state, player);

        var character = Character.Create(state, FindEntry(state), modelFactory, rigTable, actionSetTable, itemTable,
            shipTable, shipPartTable, mapGrid, nameplate);

        characterRoot.AddChild(character);

        _characters[state.WorldId] = character;

        if (state.WorldId != player.WorldId)
        {
            _followedMotions[state.WorldId] = new FollowedMotion(character);
        }

        return character;
    }

    public Character? Find(uint worldId)
    {
        return _characters.GetValueOrDefault(worldId);
    }

    public void Follow(uint worldId, IReadOnlyList<System.Numerics.Vector2> points, MoveStates moveState)
    {
        if (_followedMotions.TryGetValue(worldId, out var followedMotion))
        {
            followedMotion.Follow(points, moveState);
        }
    }

    public void ChangePlayer(CharacterState player)
    {
        foreach (var character in _characters.Values)
        {
            character.ChangePlayer(player);
        }
    }

    public void Remove(uint worldId)
    {
        _followedMotions.Remove(worldId);

        if (!_characters.Remove(worldId, out var character))
        {
            return;
        }

        characterRoot.RemoveChild(character);
        character.QueueFree();
    }

    private CharacterEntry FindEntry(CharacterState state)
    {
        return characterTable.TryGetById(state.TypeId, out var entry)
            ? entry
            : throw new KeyNotFoundException(
                $"the character table has no row {state.TypeId} for {state.Name}");
    }
}
