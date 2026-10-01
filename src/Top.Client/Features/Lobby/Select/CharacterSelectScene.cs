using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Top.Client.Core;
using Top.Client.Features.World;
using Top.Client.Messages;
using Top.Client.Messages.Account.Data;
using Top.Logging;

namespace Top.Client.Features.Lobby.Select;

public partial class CharacterSelectScene : Node
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly List<CharacterStand> _stands = [];

    private readonly List<CharacterSlot> _characters = [];

    private Transform3D _restingCamera;

    private CharacterStand? _selected;

    private Session _session = null!;

    private IServerConnection _connection = null!;

    private ServerContent _content = null!;

    private SceneRouter _router = null!;

    private CharacterSelectPayload _payload = null!;

    [Export] public CharacterSelectScreen CharacterSelectScreen { get; set; } = null!;

    [Export] public Camera3D Camera { get; set; } = null!;

    [Export] public Node3D Stands { get; set; } = null!;

    [Export] public DirectionalLight3D SelectionLight { get; set; } = null!;

    [Export] public float VerticalFovDegrees { get; set; } = 70f;

    public void Setup(Session session, IServerConnection connection, ServerContent content, SceneRouter router,
        CharacterSelectPayload payload)
    {
        _session = session;
        _connection = connection;
        _content = content;
        _router = router;
        _payload = payload;

        _characters.AddRange(payload.Characters);
    }

    public override void _Ready()
    {
        _stands.AddRange(Stands.GetChildren().OfType<CharacterStand>());

        _restingCamera = Camera.Transform;

        GetViewport().SizeChanged += KeepAspectRatio;
        KeepAspectRatio();

        foreach (var stand in _stands)
        {
            stand.Nameplate.Clicked += isDoubleClick => ClickStand(stand, isDoubleClick);
        }

        CharacterSelectScreen.Closed += GoBack;
        CharacterSelectScreen.EnterRequested += BeginPlay;
        CharacterSelectScreen.SelectionMoveRequested += MoveSelection;
        CharacterSelectScreen.MapEntered += GoToWorld;
        CharacterSelectScreen.NameplateFadeRequested += FadeNameplates;
        CharacterSelectScreen.DeleteRequested += OpenDelete;
        CharacterSelectScreen.CharacterDeleted += RemoveCharacter;

        CharacterSelectScreen.Setup(_connection, _session.CurrentAccountName ?? string.Empty, _session.CurrentServer,
            _payload.HasSecurityCode, _payload.EntryFailureText);

        ShowActions();
        ShowCharacters(0);
    }

    public override void _Process(double delta)
    {
        if (CharacterSelectScreen.IsDeleting && _selected != null)
        {
            CharacterSelectScreen.PlaceDimHole(_selected.ProjectCentre());
        }
    }

    public override void _ExitTree()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public override void _UnhandledInput(InputEvent received)
    {
        if (received is not InputEventMouseButton
            {
                ButtonIndex: MouseButton.Left, Pressed: true
            } click)
        {
            return;
        }

        var stand = PickStand(click.Position);

        if (stand == null)
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        ClickStand(stand, click.DoubleClick);
    }

    private async void ShowCharacters(int firstIndex)
    {
        try
        {
            await LoadCharacters(firstIndex, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Log.Error("the characters broke while loading, so every character stands as a placeholder", exception);

            ShowPlaceholders(firstIndex);
        }
    }

    private async Task LoadCharacters(int firstIndex, CancellationToken cancellationToken)
    {
        var characterTable = await _content.TableReader.ReadCharacters(cancellationToken);
        var rigTable = await _content.TableReader.ReadRigs(cancellationToken);
        var actionSetTable = await _content.TableReader.ReadActionSets(cancellationToken);
        var itemTable = await _content.TableReader.ReadItems(cancellationToken);

        for (var index = firstIndex; index < _stands.Count; index++)
        {
            if (index < _characters.Count)
            {
                _stands[index].ShowCharacter(_characters[index], characterTable, _content.ModelFactory, rigTable,
                    actionSetTable, itemTable);
            }
            else
            {
                _stands[index].Clear();
            }
        }
    }

    private void ShowPlaceholders(int firstIndex)
    {
        for (var index = firstIndex; index < _stands.Count; index++)
        {
            if (index < _characters.Count)
            {
                _stands[index].ShowPlaceholder(_characters[index]);
            }
            else
            {
                _stands[index].Clear();
            }
        }
    }

    private void BeginPlay()
    {
        if (_selected == null)
        {
            return;
        }

        var index = _stands.IndexOf(_selected);

        CharacterSelectScreen.BeginPlay(index, _characters[index].Name);
    }

    private void OpenDelete()
    {
        if (_selected == null)
        {
            return;
        }

        var index = _stands.IndexOf(_selected);

        CharacterSelectScreen.OpenDelete(index, _characters[index]);
    }

    private void RemoveCharacter(int characterIndex)
    {
        _characters.RemoveAt(characterIndex);

        _selected = null;

        foreach (var stand in _stands.Skip(characterIndex))
        {
            stand.Clear();
        }

        ShowActions();
        ShowCharacters(characterIndex);
    }

    private void GoToWorld()
    {
        if (CharacterSelectScreen.EnteredMap is not { } enteredMap)
        {
            return;
        }

        _router.GoToWorld(new WorldPayload(enteredMap));
    }

    private void ClickStand(CharacterStand stand, bool isDoubleClick)
    {
        if (!stand.IsOccupied || !CharacterSelectScreen.CanSelectCharacters)
        {
            return;
        }

        GetViewport().GuiReleaseFocus();

        SelectStand(stand);

        if (isDoubleClick)
        {
            BeginPlay();
        }
    }

    private void MoveSelection(int step)
    {
        var occupied = _stands.Where(stand => stand.IsOccupied).ToList();

        if (occupied.Count == 0)
        {
            return;
        }

        var index = _selected == null
            ? (step > 0 ? 0 : occupied.Count - 1)
            : Math.Clamp(occupied.IndexOf(_selected) + step, 0, occupied.Count - 1);

        SelectStand(occupied[index]);
    }

    private void SelectStand(CharacterStand stand)
    {
        if (!stand.IsOccupied || stand == _selected)
        {
            return;
        }

        _selected = stand;

        foreach (var other in _stands.Where(candidate => candidate != stand && candidate.IsOccupied))
        {
            other.Deselect();
        }

        stand.Select(SelectionLight.LightCullMask);

        ShowActions();
    }

    private void FadeNameplates(NameplateFade fade)
    {
        foreach (var stand in _stands)
        {
            stand.Nameplate.ShowFaded(fade == NameplateFade.All
                                      || (fade == NameplateFade.AllButSelected && stand != _selected));
        }
    }

    private void ShowActions()
    {
        var characterCount = _characters.Count;
        var usableSlots = Math.Min(_payload.SlotCap, _stands.Count);

        CharacterSelectScreen.ShowActions(_selected != null, characterCount < usableSlots, characterCount > 0);
    }

    private CharacterStand? PickStand(Vector2 onViewport)
    {
        var ray = PhysicsRayQueryParameters3D.Create(
            Camera.ProjectRayOrigin(onViewport),
            Camera.ProjectPosition(onViewport, Camera.Far));

        var hit = Camera.GetWorld3D().DirectSpaceState.IntersectRay(ray);

        return hit.Count > 0 ? hit["collider"].As<GodotObject>() as CharacterStand : null;
    }

    private void KeepAspectRatio()
    {
        var size = GetViewport().GetVisibleRect().Size;

        if (size.Y <= 0f)
        {
            return;
        }

        var forward = -_restingCamera.Basis.Z;
        var pullBack = CameraFraming.MeasurePullBack(
            Camera.Fov, size.X / size.Y, VerticalFovDegrees, MeasureNearestStand(forward));

        Camera.Transform = new Transform3D(_restingCamera.Basis, _restingCamera.Origin - (forward * pullBack));
    }

    private float MeasureNearestStand(Vector3 forward)
    {
        var nearest = float.MaxValue;

        foreach (var stand in _stands)
        {
            nearest = Mathf.Min(nearest, (stand.GlobalPosition - _restingCamera.Origin).Dot(forward));
        }

        return nearest == float.MaxValue ? 0f : nearest;
    }

    private void GoBack()
    {
        _router.LeaveServer();
    }
}
