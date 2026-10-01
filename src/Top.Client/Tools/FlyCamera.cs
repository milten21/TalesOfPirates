using Godot;

namespace Top.Client.Tools;

public partial class FlyCamera : Camera3D
{
    [Export] public float Speed { get; set; } = 40f;

    [Export] public float BoostFactor { get; set; } = 5f;

    [Export] public float Sensitivity { get; set; } = 0.003f;

    private float _yaw;
    private float _pitch;

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton button && button.ButtonIndex == MouseButton.Right)
        {
            Input.MouseMode = button.Pressed
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
        }

        if (inputEvent is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * Sensitivity;
            _pitch = Mathf.Clamp(_pitch - (motion.Relative.Y * Sensitivity), -1.5f, 1.5f);

            Rotation = new Vector3(_pitch, _yaw, 0f);
        }
    }

    public override void _Process(double delta)
    {
        var direction = new Vector3(
            ReadAxis(Key.D, Key.A),
            ReadAxis(Key.E, Key.Q),
            ReadAxis(Key.S, Key.W));

        if (direction == Vector3.Zero)
        {
            return;
        }

        var speed = Input.IsPhysicalKeyPressed(Key.Shift) ? Speed * BoostFactor : Speed;

        Position += Transform.Basis * direction.Normalized() * speed * (float)delta;
    }

    public void PointAt(Vector3 position, Vector3 target)
    {
        LookAtFromPosition(position, target);

        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    private static float ReadAxis(Key positive, Key negative)
    {
        return (Input.IsPhysicalKeyPressed(positive) ? 1f : 0f)
               - (Input.IsPhysicalKeyPressed(negative) ? 1f : 0f);
    }
}
