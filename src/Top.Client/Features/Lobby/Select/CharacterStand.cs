using System.Linq;
using Godot;
using Top.Client.Features.Characters;
using Top.Client.Features.Lobby.Select.Widgets;
using Top.Client.Messages.Account.Data;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Logging;

namespace Top.Client.Features.Lobby.Select;

public partial class CharacterStand : StaticBody3D
{
    private const int WaitingAction = 1;

    private const int ShowAction = 2;

    private const uint DefaultLayers = 1;

    private const float CentimetersPerMeter = 100f;

    private static readonly StringName AmbientTint = "ambient_tint";

    private static readonly Color Lit = new Color(1f, 1f, 1f);

    private static readonly Color Dark = new Color(129f / 255f, 121f / 255f, 114f / 255f);

    private StandardMaterial3D _skin = null!;

    private Shape3D _placeholderShape = null!;

    private Vector3 _placeholderShapePosition;

    private CharacterBody? _body;

    private float _headHeight;

    [Export] public MeshInstance3D Placeholder { get; set; } = null!;

    [Export] public CollisionShape3D Shape { get; set; } = null!;

    [Export] public Node3D Ring { get; set; } = null!;

    [Export] public StandNameplate Nameplate { get; set; } = null!;

    [Export] public float HeadHeight { get; set; } = 2.4f;

    [Export] public float NameplateGap { get; set; } = 80f;

    [Export] public float PoseBlendSeconds { get; set; } = 0.17f;

    public bool IsOccupied { get; private set; }

    public override void _Ready()
    {
        _skin = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Lit,
        };

        Placeholder.MaterialOverride = _skin;

        _placeholderShape = Shape.Shape;
        _placeholderShapePosition = Shape.Position;

        Clear();
    }

    public override void _Process(double delta)
    {
        if (Nameplate.Visible)
        {
            PlaceNameplate();
        }
    }

    public void ShowCharacter(CharacterSlot characterSlot, CharacterTable characterTable, ModelFactory modelFactory,
        RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable)
    {
        if (!characterTable.TryGetById(characterSlot.TypeId, out var entry))
        {
            Log.Error($"the character table has no row {characterSlot.TypeId} for {characterSlot.Name}, " +
                      "who stands as a placeholder");

            ShowPlaceholder(characterSlot);

            return;
        }

        Clear();

        // TODO: Carry the weapons on the back dummies, as the original does out of fight, when characters get a
        // fight state.
        _body = CharacterBody.Load(entry, characterSlot.Look, modelFactory, rigTable, actionSetTable, itemTable);

        AddChild(_body.Root);

        _body.PreloadActions(WaitingAction, ShowAction);
        _body.PlayAction(WaitingAction, PlayMode.Loop);

        Shape.Shape = new CapsuleShape3D
        {
            Height = entry.Height,
            Radius = Mathf.Min(entry.Radius / CentimetersPerMeter, entry.Height / 2f),
        };
        Shape.Position = Vector3.Up * (entry.Height / 2f);

        Occupy(characterSlot, entry.Height);
        Deselect();
    }

    public void ShowPlaceholder(CharacterSlot characterSlot)
    {
        Clear();

        Placeholder.Visible = true;

        Occupy(characterSlot, HeadHeight);
    }

    public void Clear()
    {
        IsOccupied = false;

        FreeBody();

        Placeholder.Visible = false;
        Shape.Shape = _placeholderShape;
        Shape.Position = _placeholderShapePosition;
        Shape.Disabled = true;
        Nameplate.Visible = false;
        Nameplate.ShowSelected(false);
        Ring.Visible = false;

        ApplyLighting(Lit, DefaultLayers);
    }

    public void Select(uint selectionLayers)
    {
        Ring.Visible = IsOccupied;
        Nameplate.ShowSelected(true);

        ApplyLighting(Lit, selectionLayers);

        _body?.PlayAction(ShowAction, PlayMode.Loop, PoseBlendSeconds);
    }

    public void Deselect()
    {
        Ring.Visible = false;
        Nameplate.ShowSelected(false);

        ApplyLighting(Dark, DefaultLayers);

        _body?.PlayAction(WaitingAction, PlayMode.Loop, PoseBlendSeconds);
    }

    public Vector2 ProjectCentre()
    {
        var camera = GetViewport().GetCamera3D();

        return camera?.UnprojectPosition(GlobalPosition + (Vector3.Up * (_headHeight * 0.5f))) ?? Vector2.Zero;
    }

    private void Occupy(CharacterSlot characterSlot, float headHeight)
    {
        IsOccupied = true;
        Shape.Disabled = false;

        _headHeight = headHeight;

        Nameplate.ShowCharacter(characterSlot.Name, $"Lv {characterSlot.Level} · {characterSlot.Job}");

        PlaceNameplate();
    }

    private void FreeBody()
    {
        if (_body == null)
        {
            return;
        }

        RemoveChild(_body.Root);
        _body.Root.QueueFree();
        _body = null;
    }

    private void ApplyLighting(Color tint, uint layers)
    {
        _skin.AlbedoColor = tint;

        if (_body == null)
        {
            return;
        }

        var ambientTint = new Vector3(tint.R, tint.G, tint.B);

        foreach (var geometry in _body.Root.FindChildren("*", nameof(GeometryInstance3D), owned: false)
                     .OfType<GeometryInstance3D>())
        {
            geometry.Layers = layers;
            geometry.SetInstanceShaderParameter(AmbientTint, ambientTint);
        }
    }

    private void PlaceNameplate()
    {
        var camera = GetViewport().GetCamera3D();

        if (camera == null)
        {
            return;
        }

        var head = camera.UnprojectPosition(GlobalPosition + (Vector3.Up * _headHeight));

        Nameplate.Position = head - (Nameplate.Size * 0.5f) - (Vector2.Down * NameplateGap);
    }
}
