using System.Collections.Generic;
using Godot;
using Top.Client.Features.Characters;
using Top.Client.Features.Maps;
using Top.Client.Features.World;

namespace Top.Client.Tools.Debug.ServerPaths;

public partial class ServerPathView : Node3D
{
    private const float WaterLevel = 0f;

    private const float RingRadius = 0.4f;

    private static readonly Color PlayerColor = Colors.Cyan;

    private static readonly Color RemainingPathColor = Colors.White;

    private static readonly Color OtherCharacterColor = Colors.Orange;

    private readonly DebugDraw _draw = new DebugDraw();

    private readonly List<Vector3> _points = [];

    private ServerPathRecorder _recorder = null!;

    private ServerPathScope _scope;

    public static ServerPathView Create(ServerPathRecorder recorder, ServerPathScope scope)
    {
        var view = new ServerPathView
        {
            Name = $"{nameof(ServerPathView)}{scope}",
            _recorder = recorder,
            _scope = scope
        };

        view.AddChild(view._draw);

        return view;
    }

    public override void _Process(double delta)
    {
        _draw.Begin();

        if (GetTree().CurrentScene is WorldScene { Player: { } player } world)
        {
            Transform = world.WorldRoot.GlobalTransform;

            DrawPaths(player, world.Map.Grid);
        }

        _draw.End();
    }

    private static Vector3 ToWorldPoint(float mapX, float mapY, MapGrid grid)
    {
        var height = Mathf.Max(grid.GroundHeightAt(mapX, mapY), WaterLevel);

        return MapSpace.ToWorld(mapX, mapY, height);
    }

    private void DrawPaths(Character player, MapGrid grid)
    {
        var playerId = player.State.WorldId;

        if (_scope == ServerPathScope.Player)
        {
            DrawRemainingPath(player, grid);
        }

        foreach (var (worldId, trail) in _recorder.Trails)
        {
            var isPlayer = worldId == playerId;

            if (isPlayer != (_scope == ServerPathScope.Player))
            {
                continue;
            }

            var color = isPlayer ? PlayerColor : OtherCharacterColor;

            DrawTrail(trail, grid, color);

            var serverPoint = trail[^1].Points[^1];
            var serverPosition = ToWorldPoint(serverPoint.X, serverPoint.Y, grid);

            _draw.AddRing(serverPosition, RingRadius, color);

            if (isPlayer)
            {
                var drift = player.Motion.MapPosition.DistanceTo(new Vector2(serverPoint.X, serverPoint.Y));

                _draw.AddText(serverPosition, $"{drift:0.00} m", color);
            }
        }
    }

    private void DrawTrail(IReadOnlyList<MoveStep> trail, MapGrid grid, Color color)
    {
        var trailSeconds = (float)ServerPathRecorder.TrailDuration.TotalSeconds;

        foreach (var step in trail)
        {
            var opacity = 1f - ((float)_recorder.MeasureAge(step).TotalSeconds / trailSeconds);

            if (opacity <= 0f)
            {
                continue;
            }

            _points.Clear();

            foreach (var point in step.Points)
            {
                _points.Add(ToWorldPoint(point.X, point.Y, grid));
            }

            _draw.AddPath(_points, color with { A = opacity });
        }
    }

    private void DrawRemainingPath(Character player, MapGrid grid)
    {
        var motion = player.Motion;

        if (motion.RemainingPoints.Count == 0)
        {
            return;
        }

        _points.Clear();
        _points.Add(ToWorldPoint(motion.MapPosition.X, motion.MapPosition.Y, grid));

        foreach (var point in motion.RemainingPoints)
        {
            _points.Add(ToWorldPoint(point.X, point.Y, grid));
        }

        _draw.AddPath(_points, RemainingPathColor);
        _draw.AddRing(_points[^1], RingRadius, RemainingPathColor);
    }
}
