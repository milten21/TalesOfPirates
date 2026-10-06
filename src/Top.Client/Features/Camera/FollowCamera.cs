using Godot;
using Top.Client.Core;
using Top.Client.UI.Cursors;

namespace Top.Client.Features.Camera;

public partial class FollowCamera : Camera3D
{
    private const float TargetHeight = 1f;

    private FollowCameraPose _pose = null!;

    private Node3D? _target;

    private InputGate? _inputGate;

    private WorldCursorService? _worldCursorService;

    [Export] public float StartDistance { get; set; } = 50f;

    [Export] public float EndDistance { get; set; } = 35f;

    [Export] public float StartHeight { get; set; } = 10f;

    [Export] public float EndHeight { get; set; } = 50f;

    [Export] public float StartFov { get; set; } = 12f;

    [Export] public float EndFov { get; set; } = 32f;

    [Export] public float DistanceShortening { get; set; } = 0.5735f;

    [Export] public float ZoomStep { get; set; } = 0.45f;

    [Export] public float SmoothingSpeed { get; set; } = 2.5f;

    [Export] public float FocusDeadZone { get; set; } = 0.5f;

    [Export] public float FocusSmoothingSpeed { get; set; } = 3f;

    [Export] public float Sensitivity { get; set; } = 0.0029f;

    [Export(PropertyHint.Range, "-180,180,0.1,radians_as_degrees")]
    public float RecenterYaw { get; set; } = -(0.5235987f + 0.0872664f);

    public override void _Ready()
    {
        _pose = new FollowCameraPose(
            new CameraRange(StartDistance, EndDistance),
            new CameraRange(StartHeight, EndHeight),
            new CameraRange(StartFov, EndFov),
            DistanceShortening,
            SmoothingSpeed,
            FocusDeadZone,
            FocusSmoothingSpeed);
    }

    public override void _UnhandledInput(InputEvent received)
    {
        if (_target != null && _inputGate is { IsBlocked: false } && TrySteer(received))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        _worldCursorService?.Release();
    }

    public override void _Process(double delta)
    {
        _pose.Advance((float)delta);

        if (_target != null)
        {
            _pose.TrackFocus(_target.GlobalPosition.Y);

            SetCameraCursor();

            Place();
        }
    }

    public void Follow(Node3D target, InputGate inputGate, WorldCursorService worldCursorService)
    {
        _target = target;
        _inputGate = inputGate;
        _worldCursorService = worldCursorService;
        _pose.PlaceFocus(target.GlobalPosition.Y);

        Place();
    }

    public void YawBy(float radians)
    {
        _pose.YawBy(radians);
    }

    public void ZoomBy(float amount)
    {
        _pose.ZoomBy(amount);
    }

    public void Recentre()
    {
        _pose.Recentre(RecenterYaw);
    }

    private static float ReadNotches(InputEvent received)
    {
        return received is InputEventMouseButton { Factor: > 0f } wheel ? wheel.Factor : 1f;
    }

    private void SetCameraCursor()
    {
        if (_inputGate!.IsActionPressed("camera_turn"))
        {
            _worldCursorService!.Hold(WorldCursor.Camera);
        }
        else
        {
            _worldCursorService!.Release();
        }
    }

    private bool TrySteer(InputEvent received)
    {
        if (received is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true, DoubleClick: true }
            && !_pose.IsDefaultView)
        {
            Recentre();

            return true;
        }

        if (received is InputEventMouseMotion motion && _inputGate!.IsActionPressed("camera_turn"))
        {
            YawBy(-motion.ScreenRelative.X * Sensitivity);

            return true;
        }

        if (received.IsActionPressed("camera_zoom_in"))
        {
            ZoomBy(-ZoomStep * ReadNotches(received));

            return true;
        }

        if (received.IsActionPressed("camera_zoom_out"))
        {
            ZoomBy(ZoomStep * ReadNotches(received));

            return true;
        }

        if (received.IsActionPressed("camera_recentre"))
        {
            Recentre();

            return true;
        }

        return false;
    }

    private void Place()
    {
        var position = _target!.GlobalPosition;
        var target = new Vector3(position.X, _pose.FocusHeight + TargetHeight, position.Z);
        var offset = _pose.EyeOffset;

        Fov = _pose.Fov;
        LookAtFromPosition(target + new Vector3(offset.X, offset.Y, offset.Z), target);
    }
}
