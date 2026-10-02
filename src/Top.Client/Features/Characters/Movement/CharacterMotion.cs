using System.Collections.Generic;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Features.Characters.Movement;

public partial class CharacterMotion : Node
{
    private const int WaitingAction = 1;

    private const int RunAction = 5;

    private const float CentimetersPerMeter = 100f;

    private const float HalfTurn = 180f;

    private const float FullTurn = 360f;

    private readonly Queue<Vector2> _points = new Queue<Vector2>();

    private Character _character = null!;

    private MapGrid _mapGrid = null!;

    private float _yaw;

    private float _targetYaw;

    private bool _isRunning;

    public float TurnRate { get; set; } = 600f;

    public float MovingTurnScale { get; set; } = 1f;

    public float Crossfade { get; set; } = 0.15f;

    public float RunReferenceSpeed { get; set; } = 4.8f;

    public Vector2 MapPosition { get; private set; }

    public float Speed { get; private set; }

    public float Rate { get; set; } = 1f;

    public IReadOnlyCollection<Vector2> RemainingPoints => _points;

    public bool IsMoving => _points.Count > 0;

    public bool IsPathOpen { get; set; }

    public float RemainingDistance
    {
        get
        {
            var distance = 0f;
            var from = MapPosition;

            foreach (var point in _points)
            {
                distance += from.DistanceTo(point);
                from = point;
            }

            return distance;
        }
    }

    private bool IsWalking => IsMoving || IsPathOpen;

    public static CharacterMotion Create(Character character, MapGrid mapGrid)
    {
        var state = character.State;
        var motion = new CharacterMotion
        {
            Name = nameof(CharacterMotion),
            _character = character,
            _mapGrid = mapGrid,
            _yaw = state.Angle,
            _targetYaw = state.Angle,
            MapPosition = new Vector2(state.Position.X, state.Position.Y),
            Speed = state.MoveSpeed,
        };

        motion.Place();

        return motion;
    }

    public override void _Process(double delta)
    {
        if (!IsWalking && !_isRunning && _yaw == _targetYaw)
        {
            return;
        }

        var seconds = (float)delta;

        Advance(seconds);
        Turn(seconds);
        Animate();
        Place();
    }

    public void Walk(IEnumerable<Vector2> points)
    {
        _points.Clear();

        Append(points);
    }

    public void Append(IEnumerable<Vector2> points)
    {
        foreach (var point in points)
        {
            _points.Enqueue(point);
        }
    }

    public void ChangeSpeed(float speed)
    {
        Speed = speed;

        if (Speed <= 0f)
        {
            _points.Clear();
            IsPathOpen = false;
        }
        else if (_isRunning)
        {
            PlayRun();
        }
    }

    public void SnapTo(Vector2 position)
    {
        _points.Clear();

        MapPosition = position;

        Place();
    }

    private static float MeasureYaw(Vector2 direction)
    {
        return Mathf.PosMod(HalfTurn - Mathf.RadToDeg(Mathf.Atan2(direction.X, direction.Y)), FullTurn);
    }

    private static float MeasureTurn(float from, float to)
    {
        return Mathf.PosMod(to - from + HalfTurn, FullTurn) - HalfTurn;
    }

    private void Advance(float seconds)
    {
        var distance = Speed * Rate * seconds;

        while (_points.TryPeek(out var point))
        {
            var remaining = MapPosition.DistanceTo(point);

            if (remaining > 0f)
            {
                _targetYaw = MeasureYaw(point - MapPosition);
            }

            if (remaining > distance)
            {
                MapPosition = MapPosition.MoveToward(point, distance);

                return;
            }

            MapPosition = point;
            distance -= remaining;
            _points.Dequeue();
        }
    }

    private void Turn(float seconds)
    {
        var turn = MeasureTurn(_yaw, _targetYaw);
        var step = TurnRate * (IsMoving ? MovingTurnScale : 1f) * seconds;

        _yaw = Mathf.Abs(turn) <= step ? _targetYaw : Mathf.PosMod(_yaw + (Mathf.Sign(turn) * step), FullTurn);
    }

    private void Animate()
    {
        if (IsWalking == _isRunning)
        {
            return;
        }

        _isRunning = IsWalking;

        if (_isRunning)
        {
            PlayRun();
        }
        else
        {
            _character.PlayAction(WaitingAction, PlayMode.Loop, Crossfade);
        }
    }

    private void PlayRun()
    {
        _character.PlayAction(RunAction, PlayMode.Loop, Crossfade, Speed / RunReferenceSpeed);
    }

    private void Place()
    {
        _character.Position = MapSpace.ToWorld(MapPosition.X, MapPosition.Y, MeasureHeight());
        _character.Rotation = new Vector3(0f, MapSpace.ToWorldYaw(_yaw), 0f);
    }

    private float MeasureHeight()
    {
        var entry = _character.Entry;
        var ground = (Territory)entry.Territory == Territory.Sea
            ? entry.SeaHeight / CentimetersPerMeter
            : _mapGrid.GroundHeightAt(MapPosition.X, MapPosition.Y);

        return ground + _character.HeightOffset;
    }
}
