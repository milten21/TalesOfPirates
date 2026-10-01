using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Features.Characters.Movement;

public class FollowedMotion
{
    private readonly CharacterMotion _motion;

    private readonly float _catchUpRate;

    private readonly float _catchUpBacklog;

    private readonly float _snapDistance;

    public FollowedMotion(Character character, float catchUpRate = 1.5f, float catchUpBacklog = 1.5f,
        float snapDistance = 20f)
    {
        _motion = character.Motion;
        _catchUpRate = catchUpRate;
        _catchUpBacklog = catchUpBacklog;
        _snapDistance = snapDistance;
    }

    public void Follow(IReadOnlyList<System.Numerics.Vector2> points, MoveStates moveState)
    {
        var serverPosition = ToMapPoint(points[^1]);

        if (_motion.Speed <= 0f || _motion.MapPosition.DistanceTo(serverPosition) > _snapDistance)
        {
            _motion.SnapTo(serverPosition);
        }
        else
        {
            _motion.Append(points.Skip(1).Select(ToMapPoint));
        }

        _motion.IsPathOpen = moveState == MoveStates.None;
        _motion.Rate = _motion.RemainingDistance > _catchUpBacklog * _motion.Speed ? _catchUpRate : 1f;
    }

    private static Vector2 ToMapPoint(System.Numerics.Vector2 contractPoint)
    {
        return new Vector2(contractPoint.X, contractPoint.Y);
    }
}
