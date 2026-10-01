using Godot;
using Top.Client.Core;
using Top.Client.Features.Maps;

namespace Top.Client.Features.Characters.Movement;

public partial class PlayerController : Node
{
    private static readonly StringName MoveAction = "move";

    private Camera3D _camera = null!;

    private MapGrid _mapGrid = null!;

    private GroundPicker _groundPicker = null!;

    private InputGate _inputGate = null!;

    private PredictedMotion _predictedMotion = null!;

    private Vector2 _lastTarget;

    private double _repeatEnd;

    private MoveInputState _state;

    private double _stateCountdown;

    [Export] public double RepeatWindow { get; set; } = 0.1;

    [Export] public double HoldDuration { get; set; } = 0.2;

    [Export] public double FollowInterval { get; set; } = 0.5;

    [Export] public ClickMarker ClickMarker { get; set; } = null!;

    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledInput(false);
    }

    public void Setup(Camera3D camera, Node3D worldRoot, MapGrid mapGrid, PredictedMotion predictedMotion,
        InputGate inputGate)
    {
        _camera = camera;
        _mapGrid = mapGrid;
        _groundPicker = new GroundPicker(mapGrid, worldRoot);
        _inputGate = inputGate;
        _predictedMotion = predictedMotion;
        _state = MoveInputState.Idle;

        SetProcess(true);
        SetProcessUnhandledInput(true);
    }

    public override void _UnhandledInput(InputEvent received)
    {
        if (_inputGate.IsBlocked || received is not InputEventMouseButton { Pressed: true } press
            || IsWheel(press.ButtonIndex))
        {
            return;
        }

        var isMovePress = press.IsAction(MoveAction);

        if (_state == MoveInputState.FollowingCursor)
        {
            _state = MoveInputState.Idle;

            if (!isMovePress)
            {
                _predictedMotion.Stop();
            }
        }

        if (!isMovePress)
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        _state = MoveInputState.Pressed;
        _stateCountdown = HoldDuration;

        MoveToClick(press.Position);
    }

    public override void _Process(double delta)
    {
        switch (_state)
        {
            case MoveInputState.Pressed:
                WaitForHold(delta);
                break;
            case MoveInputState.FollowingCursor:
                FollowCursor(delta);
                break;
        }
    }

    private static bool IsWheel(MouseButton button)
    {
        return button is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.WheelLeft
            or MouseButton.WheelRight;
    }

    private void MoveToClick(Vector2 screenPosition)
    {
        var now = Time.GetTicksMsec() / 1000.0;

        ClickMarker.Hide();

        if (!_groundPicker.TryPick(_camera, screenPosition, out var target))
        {
            return;
        }

        ClickMarker.ShowAt(_mapGrid.GroundPointAt(target));

        if (IsRepeat(target, now))
        {
            return;
        }

        _repeatEnd = now + RepeatWindow;
        _lastTarget = target;

        _predictedMotion.MoveTo(target);
    }

    private bool IsRepeat(Vector2 target, double now)
    {
        return now <= _repeatEnd && target.IsEqualApprox(_lastTarget);
    }

    private void WaitForHold(double delta)
    {
        if (!_inputGate.IsActionPressed(MoveAction))
        {
            _state = MoveInputState.Idle;

            return;
        }

        _stateCountdown -= delta;

        if (_stateCountdown > 0)
        {
            return;
        }

        _state = MoveInputState.FollowingCursor;
        _stateCountdown = 0;

        ClickMarker.Hide();
    }

    private void FollowCursor(double delta)
    {
        _stateCountdown -= delta;

        if (_stateCountdown > 0 || _inputGate.IsBlocked)
        {
            return;
        }

        _stateCountdown = FollowInterval;

        var viewport = GetViewport();

        if (viewport.GuiGetHoveredControl() == null
            && _groundPicker.TryPick(_camera, viewport.GetMousePosition(), out var point))
        {
            _predictedMotion.RunToward(point);
        }
    }
}
