using System.Linq;
using Godot;
using Top.Client.Features.Maps;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Logging;

namespace Top.Client.Features.Characters.Portrait;

public partial class CharacterPortrait : SubViewportContainer
{
    private const int WaitingAction = 1;

    private const float FacingYaw = 180f;

    private const float EyeHeight = 0.4f;

    private const float PlayerFieldOfView = Mathf.Pi * 0.12f;

    private const float MonsterFieldOfView = Mathf.Pi * 0.14f;

    private const float DeadBrightness = 0.6f;

    private static readonly Vector3 DefaultSunDirection = MapSpace.ToWorld(1f, 1f, -1f);

    private static readonly StringName SaturationParameter = "saturation";

    private static readonly StringName BrightnessParameter = "brightness";

    private CharacterTable _characterTable = null!;

    private PortraitCameraTable _portraitCameraTable = null!;

    private ModelFactory _modelFactory = null!;

    private RigTable _rigTable = null!;

    private ActionSetTable _actionSetTable = null!;

    private ItemTable _itemTable = null!;

    private CharacterBody? _body;

    private int? _typeId;

    private CharacterLook? _look;

    [Export] public Camera3D Camera { get; set; } = null!;

    [Export] public DirectionalLight3D Light { get; set; } = null!;

    [Export] public Node3D Stage { get; set; } = null!;

    public override void _Ready()
    {
        Light.Basis = Basis.LookingAt(DefaultSunDirection);
    }

    public void Setup(CharacterTable characterTable, PortraitCameraTable portraitCameraTable,
        ModelFactory modelFactory, RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable)
    {
        _characterTable = characterTable;
        _portraitCameraTable = portraitCameraTable;
        _modelFactory = modelFactory;
        _rigTable = rigTable;
        _actionSetTable = actionSetTable;
        _itemTable = itemTable;
    }

    public void ShowCharacter(int typeId, CharacterLook? look)
    {
        if (typeId == _typeId)
        {
            ChangeLook(look);

            return;
        }

        RemoveBody();

        _typeId = typeId;

        if (!_characterTable.TryGetById(typeId, out var entry))
        {
            Log.Warning($"the character table has no row {typeId}, the portrait shows no character");

            return;
        }

        if (_portraitCameraTable.FindCamera(typeId) is not { } framing)
        {
            Log.Warning($"the portrait camera table has no row {typeId} and no row 0, the portrait shows no character");

            return;
        }

        var body = CharacterBody.Load(entry, look, _modelFactory, _rigTable, _actionSetTable, _itemTable,
            holdsItems: false);

        body.Root.Position = Vector3.Up * (framing.Height - entry.Height);
        body.Root.Rotation = new Vector3(0f, MapSpace.ToWorldYaw(FacingYaw), 0f);

        Stage.AddChild(body.Root);

        body.PlayAction(WaitingAction, PlayMode.Loop);

        PlaceCamera(framing, entry.ModalType);

        _body = body;
        _look = look;
    }

    public void SetDead(bool isDead)
    {
        var material = (ShaderMaterial)Material;

        material.SetShaderParameter(SaturationParameter, isDead ? 0f : 1f);
        material.SetShaderParameter(BrightnessParameter, isDead ? DeadBrightness : 1f);
    }

    private void ChangeLook(CharacterLook? look)
    {
        if (_body == null || look == null || IsShowing(look))
        {
            return;
        }

        _body.ChangeLook(look);
        _look = look;
    }

    private bool IsShowing(CharacterLook look)
    {
        return _look != null && _look.HairId == look.HairId && _look.EquipmentIds.SequenceEqual(look.EquipmentIds);
    }

    private void RemoveBody()
    {
        if (_body == null)
        {
            return;
        }

        Stage.RemoveChild(_body.Root);
        _body.Root.QueueFree();

        _body = null;
        _look = null;
    }

    private void PlaceCamera(PortraitCameraEntry framing, CharacterModalType modalType)
    {
        var angle = Mathf.DegToRad(framing.Angle);
        var eye = MapSpace.ToWorld(-Mathf.Sin(angle) * framing.Distance, Mathf.Cos(angle) * framing.Distance,
            EyeHeight);

        Camera.Transform = new Transform3D(Basis.LookingAt(-eye), eye);
        Camera.Fov = Mathf.RadToDeg(modalType == CharacterModalType.MainCharacter
            ? PlayerFieldOfView
            : MonsterFieldOfView);
    }
}
