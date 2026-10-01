using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Client.Rendering.Models.Gltf;
using Top.Contracts.Assets.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;
using Top.Logging;

namespace Top.Client.Features.Characters;

public class CharacterBody
{
    private const string SkeletonName = "Skeleton3D";

    private const string CapsuleName = "Capsule";

    private const string ClipLibraryName = "clips";

    private const uint GripDummy = 0;

    private const float CentimetersPerMeter = 100f;

    private const int HeadLink = 0;

    private const int BodyLinkCount = 5;

    private const int ApparelLinkOffset = 19;

    private const int LeftHandLink = 6;

    private const int RightHandLink = 9;

    private const int EmptyHandItemType = 0;

    private const int SwordItemType = 1;

    private const int GreatswordItemType = 2;

    private const int BowItemType = 3;

    private const int FiregunItemType = 4;

    private const int BladeItemType = 5;

    private const int BoxingGlovesItemType = 6;

    private const int DaggerItemType = 7;

    private const int GoldPouchItemType = 8;

    private const int ShortStaffItemType = 9;

    private const int HammerItemType = 10;

    private const int ShieldItemType = 11;

    private const int AxeItemType = 18;

    private const int MetalPickaxeItemType = 19;

    private static readonly NodePath ParentSkeleton = "..";

    private static readonly WieldStance[] WieldStances =
    [
        new WieldStance(Stance.Fists, EmptyHandItemType, GoldPouchItemType),
        new WieldStance(Stance.Fists, EmptyHandItemType, BoxingGlovesItemType),
        new WieldStance(Stance.Fists, ShieldItemType, EmptyHandItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, SwordItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, BladeItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, ShortStaffItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, HammerItemType),
        new WieldStance(Stance.Sword, ShieldItemType, SwordItemType),
        new WieldStance(Stance.TwoHandSword, EmptyHandItemType, GreatswordItemType),
        new WieldStance(Stance.DualSwords, SwordItemType, SwordItemType),
        new WieldStance(Stance.Bow, BowItemType, EmptyHandItemType),
        new WieldStance(Stance.Firegun, EmptyHandItemType, FiregunItemType),
        new WieldStance(Stance.Dagger, EmptyHandItemType, DaggerItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, AxeItemType),
        new WieldStance(Stance.Sword, EmptyHandItemType, MetalPickaxeItemType),
    ];

    private readonly CharacterEntry _entry;
    private readonly string? _contentPath;
    private readonly ModelFactory _modelFactory;
    private readonly RigEntry? _rigEntry;
    private readonly ItemTable? _itemTable;
    private readonly Rig? _rig;
    private readonly PartLayout? _partLayout;
    private readonly bool _holdsItems;

    private readonly AnimationLibrary _clips = new AnimationLibrary();

    private readonly Dictionary<int, Part> _parts = new Dictionary<int, Part>();

    private readonly Dictionary<int, RigidPart> _rigidParts = new Dictionary<int, RigidPart>();

    private readonly Dictionary<int, HeldItem> _heldItems = new Dictionary<int, HeldItem>();

    private readonly List<int> _preloadedActions = new List<int>();

    private AnimationPlayer? _animationPlayer;

    private (int Action, PlayMode Mode, float Speed)? _playing;

    private RigAction? _action;

    private RigClip? _clip;

    private string? _animationName;

    private PlayMode _mode;

    private float _speed = 1f;

    private double _position;

    private CharacterBody(Node3D root, CharacterEntry entry, string? contentPath, ModelFactory modelFactory,
        RigEntry? rigEntry, ItemTable? itemTable, Rig? rig, PartLayout? partLayout, bool holdsItems)
    {
        Root = root;
        _entry = entry;
        _contentPath = contentPath;
        _modelFactory = modelFactory;
        _rigEntry = rigEntry;
        _itemTable = itemTable;
        _rig = rig;
        _partLayout = partLayout;
        _holdsItems = holdsItems;
    }

    public event Action<int, string>? ActionEvent;

    public Node3D Root { get; }

    private Stance Stance => SelectStance(GetHeldItemType(LeftHandLink), GetHeldItemType(RightHandLink));

    public static CharacterBody Load(CharacterEntry entry, CharacterLook? look, ModelFactory modelFactory,
        RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable, bool holdsItems = true)
    {
        var rigEntry = FindRig(entry, rigTable, actionSetTable);

        return LoadOrCreateCapsule(entry, modelFactory,
            () => LoadBody(entry, look, modelFactory, rigEntry, itemTable, holdsItems));
    }

    public static CharacterBody LoadParts(CharacterEntry entry, IReadOnlyList<string?> partPaths,
        ModelFactory modelFactory, ActionSetTable actionSetTable)
    {
        var rigEntry = FindActionSet(entry, actionSetTable);

        return LoadOrCreateCapsule(entry, modelFactory, () => FromParts(entry, partPaths, modelFactory, rigEntry));
    }

    public void ChangeLook(CharacterLook look)
    {
        if (_rig == null || _entry.BodyType is not { } bodyType)
        {
            return;
        }

        var stance = Stance;

        ShowLook(look, bodyType);

        if (Stance == stance)
        {
            return;
        }

        LoadClips(_preloadedActions);

        if (_playing is { } playing)
        {
            PlayAction(playing.Action, playing.Mode, speed: playing.Speed);
        }
    }

    public void PlayAction(int action, PlayMode mode, float blend = 0f, float speed = 1f)
    {
        if (_rigEntry == null)
        {
            return;
        }

        _playing = (action, mode, speed);

        var stance = Stance;

        if (_rigEntry.FindAction(action, stance) is not { } row || _rigEntry.FindClip(row) is not { } clip)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) has no clip for action {action} " +
                        $"in stance {stance} in rig {_rigEntry.Id}");

            return;
        }

        Play(row, clip, mode, blend, speed);
    }

    public void PreloadActions(params int[] actions)
    {
        foreach (var action in actions)
        {
            if (!_preloadedActions.Contains(action))
            {
                _preloadedActions.Add(action);
            }
        }

        LoadClips(actions);
    }

    public void ShowParts(IReadOnlyList<string?> partPaths)
    {
        if (_partLayout is not { } layout)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) is not built of parts and shows none");

            return;
        }

        var takenOff = new List<RigidPart>();
        var slotCount = Math.Max(partPaths.Count, _rigidParts.Count == 0 ? 0 : _rigidParts.Keys.Max() + 1);

        for (var slot = 0; slot < slotCount; slot++)
        {
            var modelPath = slot < partPaths.Count ? partPaths[slot] : null;

            if (_rigidParts.TryGetValue(slot, out var shown))
            {
                if (shown.ModelPath == modelPath)
                {
                    continue;
                }

                _rigidParts.Remove(slot);
                takenOff.Add(shown);
            }

            if (modelPath != null && LoadRigidPart(slot, modelPath, slot == layout.DrivingSlot) is { } part)
            {
                _rigidParts[slot] = part;
            }
        }

        PlaceRigidParts(layout);
        DriveWith(_rigidParts.TryGetValue(layout.DrivingSlot, out var driving) ? driving.Player : null);

        foreach (var part in takenOff)
        {
            part.Root.GetParent()?.RemoveChild(part.Root);
        }

        foreach (var part in takenOff)
        {
            part.Root.Free();
        }
    }

    private static CharacterBody LoadOrCreateCapsule(CharacterEntry entry, ModelFactory modelFactory,
        Func<CharacterBody?> load)
    {
        try
        {
            if (load() is { } body)
            {
                return body;
            }

            Log.Warning($"{entry.Name} (type {entry.Id}) has no body that loads and stands as a capsule");
        }
        catch (Exception exception)
        {
            Log.Error($"the body of {entry.Name} (type {entry.Id}) broke while loading and stands as a capsule",
                exception);
        }

        return CreateCapsule(entry, modelFactory);
    }

    private static CharacterBody? LoadBody(CharacterEntry entry, CharacterLook? look, ModelFactory modelFactory,
        RigEntry? rigEntry, ItemTable itemTable, bool holdsItems)
    {
        if (rigEntry is { Path.Length: > 0 } && entry.BodyType is { } bodyType)
        {
            return FromRig(entry, look ?? new CharacterLook(0, entry.DefaultItems ?? []), modelFactory, rigEntry,
                itemTable, bodyType, holdsItems);
        }

        if (entry.PartPaths is { Length: > 0 })
        {
            return FromParts(entry, entry.PartPaths, modelFactory, rigEntry);
        }

        return string.IsNullOrEmpty(entry.ModelPath) ? null : FromModel(entry, modelFactory, rigEntry);
    }

    private static CharacterBody? FromRig(CharacterEntry entry, CharacterLook look, ModelFactory modelFactory,
        RigEntry rigEntry, ItemTable itemTable, int bodyType, bool holdsItems)
    {
        if (modelFactory.InstantiateRig(rigEntry.Path) is not { } instance)
        {
            return null;
        }

        if (instance.Root.GetNodeOrNull<Skeleton3D>(SkeletonName) is not { } skeleton)
        {
            Log.Error($"the rig {rigEntry.Path} of {entry.Name} (type {entry.Id}) " +
                      $"has no {SkeletonName} under its root");

            instance.Root.Free();

            return null;
        }

        var body = new CharacterBody(instance.Root, entry, rigEntry.Path, modelFactory, rigEntry, itemTable,
            new Rig(skeleton, instance.Skin), null, holdsItems).DriveWithRootPlayer();

        body.ShowLook(look, bodyType);

        return body;
    }

    private static CharacterBody? FromModel(CharacterEntry entry, ModelFactory modelFactory, RigEntry? rigEntry)
    {
        return modelFactory.InstantiateCharacter(entry.ModelPath) is { } model
            ? new CharacterBody(model, entry, entry.ModelPath, modelFactory, rigEntry, null, null, null, false)
                .DriveWithRootPlayer()
            : null;
    }

    private static CharacterBody FromParts(CharacterEntry entry, IReadOnlyList<string?> partPaths,
        ModelFactory modelFactory, RigEntry? rigEntry)
    {
        var partLayout = entry.ModalType == CharacterModalType.Boat
            ? new PartLayout(BoatSlots.Driving, BoatSlots.ParentOf)
            : new PartLayout(TowerSlots.Driving, TowerSlots.ParentOf);
        var body = new CharacterBody(new Node3D(), entry, null, modelFactory, rigEntry, null, null, partLayout, false);

        body.ShowParts(partPaths);

        return body;
    }

    private static CharacterBody CreateCapsule(CharacterEntry entry, ModelFactory modelFactory)
    {
        var height = entry.Height;
        var radius = Mathf.Min(entry.Radius / CentimetersPerMeter, height / 2f);
        var root = new Node3D();

        root.AddChild(new MeshInstance3D
        {
            Name = CapsuleName,
            Mesh = new CapsuleMesh { Height = height, Radius = radius },
            Position = Vector3.Up * (height / 2f),
        });

        return new CharacterBody(root, entry, null, modelFactory, null, null, null, null, false);
    }

    private static RigEntry? FindRig(CharacterEntry entry, RigTable rigTable, ActionSetTable actionSetTable)
    {
        if (entry.RigId is not { } rigId)
        {
            return FindActionSet(entry, actionSetTable);
        }

        if (rigTable.TryGetById(rigId, out var rig))
        {
            return rig;
        }

        Log.Warning($"{entry.Name} (type {entry.Id}) animates through rig {rigId}, which the rig table does not carry");

        return null;
    }

    private static RigEntry? FindActionSet(CharacterEntry entry, ActionSetTable actionSetTable)
    {
        if (entry.ActionSetId is not { } actionSetId)
        {
            return null;
        }

        if (actionSetTable.TryGetById(actionSetId, out var actionSet))
        {
            return actionSet;
        }

        Log.Warning($"{entry.Name} (type {entry.Id}) plays action set {actionSetId}, " +
                    "which the action set table does not carry");

        return null;
    }

    private static int GetEquipmentAt(CharacterLook look, int link)
    {
        return link < look.EquipmentIds.Count ? look.EquipmentIds[link] : 0;
    }

    private static Stance SelectStance(int leftHandItemType, int rightHandItemType)
    {
        var index = Array.FindIndex(WieldStances, wieldStance => wieldStance.Matches(leftHandItemType, rightHandItemType));

        return index >= 0 ? WieldStances[index].Stance : Stance.Fists;
    }

    private static AnimationPlayer AddAnimationPlayer(Node3D root)
    {
        var player = new AnimationPlayer { Name = TopAnimationExtension.AnimationPlayerName };

        root.AddChild(player);

        return player;
    }

    private static void PlayOverAndOver(AnimationPlayer player)
    {
        if (player.GetAnimationList() is not [var animationName, ..])
        {
            return;
        }

        player.AnimationFinished += finished => player.Play(finished);
        player.Play(animationName);
    }

    private static IReadOnlyList<MeshInstance3D> FindPartMeshes(Node model)
    {
        return model.FindChildren("*", nameof(MeshInstance3D), owned: false)
            .Cast<MeshInstance3D>()
            .Where(mesh => mesh.GetParent() is not MeshInstance3D)
            .ToList();
    }

    private static void BindToSkeleton(MeshInstance3D mesh, Rig rig)
    {
        mesh.GetParent().RemoveChild(mesh);
        mesh.Owner = null;

        if (mesh.Skin != null && HasSameBinds(mesh.Skin, rig.Skin))
        {
            mesh.Skin = rig.Skin;
        }

        rig.Skeleton.AddChild(mesh);
        mesh.Skeleton = ParentSkeleton;
    }

    private static bool HasSameBinds(Skin partSkin, Skin rigSkin)
    {
        if (partSkin.GetBindCount() != rigSkin.GetBindCount())
        {
            return false;
        }

        for (var bind = 0; bind < rigSkin.GetBindCount(); bind++)
        {
            if (partSkin.GetBindName(bind) != rigSkin.GetBindName(bind)
                || !partSkin.GetBindPose(bind).IsEqualApprox(rigSkin.GetBindPose(bind)))
            {
                return false;
            }
        }

        return true;
    }

    private void ShowLook(CharacterLook look, int bodyType)
    {
        for (var link = 0; link < BodyLinkCount; link++)
        {
            WearLink(link, look, bodyType);
        }

        if (!_holdsItems)
        {
            return;
        }

        HoldLink(RightHandLink, GetEquipmentAt(look, RightHandLink), bodyType);
        HoldLink(LeftHandLink, GetEquipmentAt(look, LeftHandLink), bodyType);
    }

    private void WearLink(int link, CharacterLook look, int bodyType)
    {
        var apparelLink = link + ApparelLinkOffset;

        if (TryWear(link, GetEquipmentAt(look, apparelLink), apparelLink, bodyType)
            || TryWear(link, GetEquipmentAt(look, link), link, bodyType)
            || (link == HeadLink && TryWear(link, look.HairId, link, bodyType))
            || TryWear(link, GetDefaultItemAt(link), link, bodyType))
        {
            return;
        }

        TakeOff(link);
    }

    private bool TryWear(int partLink, int itemId, int itemLink, int bodyType)
    {
        return FindItem(itemId) is { } item
               && FindModelPath(item, itemLink, bodyType) is { } modelPath
               && Wear(partLink, modelPath);
    }

    private void HoldLink(int link, int itemId, int bodyType)
    {
        if (FindItem(itemId) is { } item
            && FindModelPath(item, link, bodyType) is { } modelPath
            && Hold(link, modelPath, item.Type))
        {
            return;
        }

        Release(link);
    }

    private int GetHeldItemType(int link)
    {
        return _heldItems.TryGetValue(link, out var held) ? held.ItemType : EmptyHandItemType;
    }

    private int GetDefaultItemAt(int link)
    {
        return _entry.DefaultItems is { } items && link < items.Length ? items[link] : 0;
    }

    private ItemEntry? FindItem(int itemId)
    {
        if (itemId == 0 || _itemTable == null)
        {
            return null;
        }

        if (!_itemTable.TryGetById(itemId, out var item))
        {
            Log.Info($"{_entry.Name} (type {_entry.Id}) shows item {itemId}, which the item table does not carry");

            return null;
        }

        return item;
    }

    private string? FindModelPath(ItemEntry item, int link, int bodyType)
    {
        if (!item.CanEquipAt(link))
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) shows item {item.Id} at link {link}, " +
                        $"which is not one of its equip links");

            return null;
        }

        var modelPath = item.ModelPathFor(bodyType);

        if (modelPath == null)
        {
            Log.Info($"{_entry.Name} (type {_entry.Id}) shows item {item.Id}, " +
                     $"which has no model for body type {bodyType}");
        }

        return modelPath;
    }

    private bool Wear(int link, string modelPath)
    {
        if (_rig == null)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) has no rig and cannot wear {modelPath}");

            return false;
        }

        if (_parts.TryGetValue(link, out var worn) && worn.ModelPath == modelPath)
        {
            return true;
        }

        if (_modelFactory.InstantiateCharacter(modelPath) is not { } model)
        {
            Log.Warning($"the part {modelPath} for link {link} of {_entry.Name} (type {_entry.Id}) did not load");

            return false;
        }

        TakeOff(link);

        var meshes = FindPartMeshes(model);

        foreach (var mesh in meshes)
        {
            BindToSkeleton(mesh, _rig);
        }

        model.Free();

        _parts[link] = new Part(modelPath, meshes);

        return true;
    }

    private void TakeOff(int link)
    {
        if (!_parts.Remove(link, out var part))
        {
            return;
        }

        foreach (var mesh in part.Meshes)
        {
            mesh.GetParent().RemoveChild(mesh);
            mesh.Free();
        }
    }

    private bool Hold(int link, string modelPath, int itemType)
    {
        if (_heldItems.TryGetValue(link, out var held) && held.ModelPath == modelPath)
        {
            _heldItems[link] = held with { ItemType = itemType };

            return true;
        }

        var dummyName = Naming.Dummy((uint)link);

        if (Root.FindChild(dummyName, recursive: true, owned: false) is not Node3D dummy)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) has no {dummyName} to hold {modelPath}");

            return false;
        }

        if (_modelFactory.Instantiate(modelPath) is not { } model)
        {
            Log.Warning($"the held item {modelPath} for link {link} of {_entry.Name} (type {_entry.Id}) " +
                        "did not load");

            return false;
        }

        Release(link);

        if (model.GetNodeOrNull<Node3D>(Naming.Dummy(GripDummy)) is { } grip)
        {
            model.Transform = grip.Transform.AffineInverse();
        }

        dummy.AddChild(model);

        _heldItems[link] = new HeldItem(modelPath, itemType, model);

        return true;
    }

    private void Release(int link)
    {
        if (!_heldItems.Remove(link, out var held))
        {
            return;
        }

        held.Model.GetParent().RemoveChild(held.Model);
        held.Model.Free();
    }

    private void LoadClips(IEnumerable<int> actions)
    {
        if (_rigEntry == null)
        {
            return;
        }

        var stance = Stance;

        foreach (var action in actions)
        {
            if (_rigEntry.FindClip(action, stance) is { } clip)
            {
                AddAnimation(clip);
            }
        }
    }

    private void Play(RigAction action, RigClip clip, PlayMode mode, float blend, float speed)
    {
        if (_animationPlayer == null)
        {
            _action = action;
            _clip = clip;
            _animationName = null;
            _mode = mode;
            _speed = speed;

            return;
        }

        if (AddAnimation(clip) is not { } animationName)
        {
            return;
        }

        var isContinued = _action?.Id == action.Id
                          && _animationName == animationName
                          && _animationPlayer.IsPlaying();
        var isSpeedChanged = speed != _speed;

        _action = action;
        _clip = clip;
        _animationName = animationName;
        _mode = mode;
        _speed = speed;

        if (isContinued)
        {
            if (isSpeedChanged)
            {
                _animationPlayer.Play(animationName, customBlend: 0, customSpeed: speed);
            }

            return;
        }

        _animationPlayer.Play(animationName, blend, speed);
        _position = 0;
        _animationPlayer.Seek(0, update: true);
        Fire(action, RigEvent.Begin);
    }

    private CharacterBody DriveWithRootPlayer()
    {
        DriveWith(Root.GetNodeOrNull<AnimationPlayer>(TopAnimationExtension.AnimationPlayerName)
                  ?? AddAnimationPlayer(Root));

        return this;
    }

    private void DriveWith(AnimationPlayer? player)
    {
        if (player == _animationPlayer)
        {
            return;
        }

        if (_animationPlayer != null)
        {
            _animationPlayer.MixerApplied -= AdvancePlayback;
            _animationPlayer.AnimationFinished -= FinishPlayback;
        }

        _animationPlayer = player;

        if (player == null)
        {
            return;
        }

        if (!player.HasAnimationLibrary(ClipLibraryName))
        {
            player.AddAnimationLibrary(ClipLibraryName, _clips);
        }

        player.MixerApplied += AdvancePlayback;
        player.AnimationFinished += FinishPlayback;

        if (_action != null && _clip != null)
        {
            Play(_action, _clip, _mode, 0f, _speed);
        }
    }

    private RigidPart? LoadRigidPart(int slot, string modelPath, bool isDriving)
    {
        if (_modelFactory.InstantiateCharacter(modelPath) is not { } root)
        {
            Log.Warning($"the part {modelPath} for slot {slot} of {_entry.Name} (type {_entry.Id}) did not load");

            return null;
        }

        var node = root.GetChildren().OfType<Node3D>().FirstOrDefault(child => Naming.IsGeometry(child.Name)) ?? root;
        var player = root.GetNodeOrNull<AnimationPlayer>(TopAnimationExtension.AnimationPlayerName);

        if (!isDriving && player != null)
        {
            PlayOverAndOver(player);
        }

        return new RigidPart(modelPath, root, node, player);
    }

    private void PlaceRigidParts(PartLayout layout)
    {
        foreach (var (slot, part) in _rigidParts)
        {
            var parent = layout.ParentSlotOf(slot) is { } parentSlot
                         && _rigidParts.TryGetValue(parentSlot, out var parentPart)
                ? parentPart.Node
                : Root;

            if (part.Root.GetParent() == parent)
            {
                continue;
            }

            part.Root.GetParent()?.RemoveChild(part.Root);
            parent.AddChild(part.Root);
        }
    }

    private string? AddAnimation(RigClip clip)
    {
        if (_animationPlayer == null)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) has no part that plays actions and plays no {clip.Name}");

            return null;
        }

        if (clip.File == null || clip.File == _contentPath)
        {
            if (_animationPlayer.HasAnimation(clip.Animation))
            {
                return clip.Animation;
            }

            Log.Warning($"{_entry.Name} (type {_entry.Id}) has no animation {clip.Animation} in {_contentPath}");

            return null;
        }

        var animationName = $"{ClipLibraryName}/{clip.Name}";

        if (_clips.HasAnimation(clip.Name))
        {
            return animationName;
        }

        if (_modelFactory.LoadAnimation(clip.File, clip.Animation) is not { } animation)
        {
            Log.Warning($"{_entry.Name} (type {_entry.Id}) found no animation {clip.Animation} in {clip.File}");

            return null;
        }

        _clips.AddAnimation(clip.Name, animation);

        return animationName;
    }

    private void AdvancePlayback()
    {
        if (_action is not { } action || _animationName is not { } animationName
            || _animationPlayer?.CurrentAnimation != animationName)
        {
            return;
        }

        var from = _position;

        _position = _animationPlayer.CurrentAnimationPosition;
        FireEventsBetween(action, from, _position);
    }

    private void FinishPlayback(StringName animationName)
    {
        if (_action is not { } action || _animationName != animationName.ToString())
        {
            return;
        }

        var from = _position;

        _position = 0;
        FireEventsBetween(action, from, double.PositiveInfinity);
        Fire(action, RigEvent.End);

        if (_action != action || _mode != PlayMode.Loop || _animationPlayer == null || _animationPlayer.IsPlaying())
        {
            return;
        }

        _animationPlayer.Play(animationName, customSpeed: _speed);
        Fire(action, RigEvent.Begin);
    }

    private void FireEventsBetween(RigAction action, double from, double to)
    {
        foreach (var rigEvent in action.Events ?? [])
        {
            if (rigEvent.Time >= from && rigEvent.Time < to)
            {
                Fire(action, rigEvent.Name);
            }
        }
    }

    private void Fire(RigAction action, string name)
    {
        ActionEvent?.Invoke(action.Id, name);
    }

    private readonly record struct WieldStance(Stance Stance, int LeftHandItemType, int RightHandItemType)
    {
        public bool Matches(int leftHandItemType, int rightHandItemType)
        {
            return LeftHandItemType == leftHandItemType && RightHandItemType == rightHandItemType;
        }
    }

    private record Rig(Skeleton3D Skeleton, Skin Skin);

    private record Part(string ModelPath, IReadOnlyList<MeshInstance3D> Meshes);

    private record HeldItem(string ModelPath, int ItemType, Node3D Model);

    private record PartLayout(int DrivingSlot, Func<int, int?> ParentSlotOf);

    private record RigidPart(string ModelPath, Node3D Root, Node3D Node, AnimationPlayer? Player);
}
