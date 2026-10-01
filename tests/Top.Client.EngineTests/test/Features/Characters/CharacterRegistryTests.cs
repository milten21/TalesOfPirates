using System;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Top.Client.Core;
using Top.Client.Features.Characters;
using Top.Client.Features.Maps;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models.Gltf;
using static GdUnit4.Assertions;

namespace Top.Client.EngineTests.Test.Features.Characters;

[TestSuite]
[RequireGodotRuntime]
public class CharacterRegistryTests
{
    private const int LanceTypeId = 1;

    private const int ArgentMapId = 1;

    private const string IdleAnimation = "clips/01_waiting";

    private const int FinishWaitInClipLengths = 2;

    private Node3D _characterRoot = null!;

    [BeforeTest]
    public void AddCharacterRoot()
    {
        _characterRoot = AutoFree(new Node3D())!;

        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(_characterRoot);
    }

    [TestCase]
    public async Task Added_character_loops_its_idle_by_replay()
    {
        var registry = await CreateRegistry();
        var state = CreatePlayerState(LanceTypeId);

        var character = registry.Add(state, state);

        var player = character.GetNode("Body").GetNode<AnimationPlayer>(TopAnimationExtension.AnimationPlayerName);

        AssertThat(player.CurrentAnimation).IsEqual(IdleAnimation);
        AssertThat(player.GetAnimation(IdleAnimation).LoopMode).IsEqual(Animation.LoopModeEnum.None);

        var clipLength = player.GetAnimation(IdleAnimation).Length;
        var finishTimeout = TimeSpan.FromSeconds(clipLength * FinishWaitInClipLengths);

        await AssertSignal(player).IsEmitted(AnimationMixer.SignalName.AnimationFinished, new StringName(IdleAnimation))
            .WithTimeout((int)finishTimeout.TotalMilliseconds);

        AssertThat(player.IsPlaying()).IsTrue();
        AssertThat(player.CurrentAnimation).IsEqual(IdleAnimation);
        AssertThat(player.CurrentAnimationPosition).IsLess(clipLength);
    }

    private static CharacterState CreatePlayerState(int typeId)
    {
        return new CharacterState(1, CharacterControlType.Player, typeId, "Tester", System.Numerics.Vector2.Zero, 0,
            0, 0, 0, string.Empty, string.Empty, 0, 1, 100, 100, 100, 100, 0, 0, 0, 0, 0, 4.8f, null, null);
    }

    private async Task<CharacterRegistry> CreateRegistry()
    {
        var content = TestContent.ServerContent;
        var tables = content.TableReader;
        var source = content.ContentRoot.Source;
        var mapEntry = new MapLookup(await tables.ReadMaps(), source).FindById(ArgentMapId);
        var camera = AutoFree(new Camera3D())!;

        _characterRoot.AddChild(camera);

        return new CharacterRegistry(_characterRoot, GD.Load<PackedScene>("res://Features/Characters/Nameplate.tscn"), camera,
            new InputGate(), await tables.ReadCharacters(), content.ModelFactory, await tables.ReadRigs(),
            await tables.ReadActionSets(), await tables.ReadItems(), await tables.ReadShips(),
            await tables.ReadShipParts(), await new MapReader(source).Read(mapEntry.MapPath));
    }
}
