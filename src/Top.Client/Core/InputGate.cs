using Godot;

namespace Top.Client.Core;

public class InputGate
{
    public bool IsBlocked { get; set; }

    public bool IsActionPressed(StringName action)
    {
        return !IsBlocked && Input.IsActionPressed(action);
    }
}
