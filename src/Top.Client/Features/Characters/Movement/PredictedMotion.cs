using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Messages;
using Top.Client.Messages.Play.Commands;
using Top.Client.Messages.Play.Data;
using Top.Logging;

namespace Top.Client.Features.Characters.Movement;

public class PredictedMotion
{
    private const float NormalRate = 0.95f;

    private const float RateRisePerSecond = 0.15f;

    private const float RateFallPerSecond = 1.5f;

    private readonly CharacterMotion _motion;

    private readonly IServerConnection _connection;

    private readonly Pathfinder _pathfinder;

    private readonly uint _worldId;

    private readonly string _name;

    private Vector2 _serverPosition;

    private Vector2 _target;

    private float _targetRate = NormalRate;

    private float _answerCountdown;

    private bool _isAwaitingServerEnd;

    private bool _isStopSent;

    private bool _isSendPending;

    private PathMode _pathMode;

    public PredictedMotion(Character character, IServerConnection connection, Pathfinder pathfinder,
        float movingTurnScale = 0.75f)
    {
        _motion = character.Motion;
        _connection = connection;
        _pathfinder = pathfinder;
        _worldId = character.State.WorldId;
        _name = character.State.Name;
        _serverPosition = _motion.MapPosition;
        _target = _motion.MapPosition;

        _motion.MovingTurnScale = movingTurnScale;
    }

    public float SnapDistance { get; set; } = 20f;

    public float MinimumPathLengthGap { get; set; } = 10f;

    public float PathLengthGapTime { get; set; } = 2f;

    public float AnswerTimeout { get; set; } = 3f;

    public float MinimumRate { get; set; } = 0.85f;

    public float MaximumRate { get; set; } = 10.3f;

    public float ServerArrivalDistance { get; set; } = 0.8f;

    public float PreMoveTime { get; set; } = 0.5f;

    public float LeadTime { get; set; } = 1f;

    public void MoveTo(Vector2 target)
    {
        if (_motion.Speed <= 0f)
        {
            return;
        }

        _pathMode = PathMode.Search;

        SnapBackIfDrifted();
        StartMove(target);
    }

    public void RunToward(Vector2 point)
    {
        if (_motion.Speed <= 0f)
        {
            return;
        }

        _pathMode = PathMode.StraightLine;

        SnapBackIfDrifted();

        var position = _motion.MapPosition;
        var leadDistance = (_motion.Speed * LeadTime) + position.DistanceTo(_serverPosition);
        var offset = point - position;

        StartMove(offset.Length() < leadDistance ? position + (offset.Normalized() * leadDistance) : point);
    }

    public void ChangeSpeed(float speed)
    {
        _motion.ChangeSpeed(speed);

        if (speed <= 0f)
        {
            _isSendPending = false;
        }
    }

    public void Stop()
    {
        _isSendPending = false;

        if (_isAwaitingServerEnd && !_isStopSent)
        {
            SendStop();
        }
    }

    public void Process(double delta)
    {
        var seconds = (float)delta;

        EaseRate(seconds);

        if (!_isAwaitingServerEnd)
        {
            return;
        }

        _answerCountdown -= seconds;

        if (_answerCountdown <= 0f)
        {
            AbandonMove("the server did not answer the move");
        }
    }

    public void Correct(System.Numerics.Vector2 serverPosition, MoveStates moveState)
    {
        _serverPosition = ToMapPoint(serverPosition);

        if (!_isStopSent)
        {
            _answerCountdown = AnswerTimeout;
        }

        if ((moveState & (MoveStates.Block | MoveStates.CannotMove)) != 0)
        {
            AbandonMove($"the server ended the move with {moveState}");
        }
        else
        {
            if (SnapBackIfDrifted())
            {
                _motion.Walk(FindPath(_serverPosition, _target).Skip(1));
            }

            if (moveState != MoveStates.None)
            {
                AcceptServerEnd(moveState);
            }
        }

        RefreshTargetRate();
    }

    public void RecoverFromFailedAction(ActionFailureReason reason)
    {
        AbandonMove($"the server refused the action ({reason})");
    }

    private static float MeasureLength(IReadOnlyList<Vector2> path)
    {
        var length = 0f;

        for (var point = 1; point < path.Count; point++)
        {
            length += path[point - 1].DistanceTo(path[point]);
        }

        return length;
    }

    private static Vector2 ToMapPoint(System.Numerics.Vector2 contractPoint)
    {
        return new Vector2(contractPoint.X, contractPoint.Y);
    }

    private static System.Numerics.Vector2 ToContractPoint(Vector2 mapPoint)
    {
        return new System.Numerics.Vector2(mapPoint.X, mapPoint.Y);
    }

    private Vector2[] FindPath(Vector2 start, Vector2 target)
    {
        return _pathMode == PathMode.StraightLine
            ? _pathfinder.FindStraightLine(start, target)
            : _pathfinder.FindPath(start, target);
    }

    private void StartMove(Vector2 target)
    {
        var localPath = FindPath(_motion.MapPosition, target);

        if (localPath.Length < 2)
        {
            return;
        }

        _target = localPath[^1];
        _motion.Walk(localPath.Skip(1));

        if (!_isAwaitingServerEnd)
        {
            SendMove();

            return;
        }

        _isSendPending = true;

        if (!_isStopSent)
        {
            SendStop();
        }

        RefreshTargetRate();
    }

    private void SendStop()
    {
        _isStopSent = true;
        _answerCountdown = AnswerTimeout;

        _connection.Send(new StopMoveCommand());
    }

    private void SendMove()
    {
        var serverPath = FindPath(_serverPosition, _target);

        _isSendPending = false;

        if (serverPath.Length < 2)
        {
            RefreshTargetRate(serverPath);

            return;
        }

        _connection.Send(new MoveCommand(_worldId, serverPath.Select(ToContractPoint).ToArray()));

        if (serverPath[^1] != _target)
        {
            _target = serverPath[^1];
            _motion.Walk(FindPath(_motion.MapPosition, _target).Skip(1));
        }

        _isAwaitingServerEnd = true;
        _answerCountdown = AnswerTimeout;

        if (Mathf.Abs(_motion.RemainingDistance - MeasureLength(serverPath)) <= MeasurePathLengthGap())
        {
            RefreshTargetRate(serverPath);

            return;
        }

        SnapToServer("its path and the server path differ too much in length");

        _motion.Walk(serverPath.Skip(1));
        _motion.Rate = NormalRate;
        _targetRate = NormalRate;
    }

    private void AcceptServerEnd(MoveStates moveState)
    {
        _isAwaitingServerEnd = false;
        _isStopSent = false;

        if (_isSendPending)
        {
            SendMove();
        }
        else if ((moveState & MoveStates.Cancel) != 0)
        {
            _target = _serverPosition;
            _motion.Walk([_target]);
        }
    }

    private void AbandonMove(string reason)
    {
        _isAwaitingServerEnd = false;
        _isStopSent = false;

        SnapToServer(reason);

        if (!_isSendPending)
        {
            _target = _serverPosition;
            _targetRate = NormalRate;

            return;
        }

        _isSendPending = false;

        StartMove(_target);
    }

    private void RefreshTargetRate(Vector2[]? serverPath = null)
    {
        if (_serverPosition.DistanceTo(_target) <= ServerArrivalDistance)
        {
            _targetRate = NormalRate;

            return;
        }

        serverPath ??= FindPath(_serverPosition, _target);

        _targetRate = MeasureRate(_motion.RemainingDistance, MeasureLength(serverPath));
    }

    private void EaseRate(float seconds)
    {
        _motion.Rate = _motion.Rate > _targetRate
            ? Mathf.Max(_motion.Rate - (RateFallPerSecond * seconds), _targetRate)
            : Mathf.Min(_motion.Rate + (RateRisePerSecond * seconds), _targetRate);
    }

    private float MeasureRate(float localLength, float serverLength)
    {
        return serverLength > 0f
            ? Mathf.Clamp(localLength / serverLength / NormalRate, MinimumRate, MaximumRate)
            : NormalRate;
    }

    private float MeasurePathLengthGap()
    {
        return Mathf.Max(MinimumPathLengthGap, _motion.Speed * (PathLengthGapTime + PreMoveTime));
    }

    private bool SnapBackIfDrifted()
    {
        if (_motion.MapPosition.DistanceTo(_serverPosition) <= SnapDistance)
        {
            return false;
        }

        SnapToServer("it drifted too far from the server");

        return true;
    }

    private void SnapToServer(string reason)
    {
        Log.Info($"{_name} snapped {_motion.MapPosition.DistanceTo(_serverPosition):0.00} m back to the server "
                 + $"position, because {reason}");

        _motion.SnapTo(_serverPosition);
    }
}
