using Godot;

namespace Top.Client.Tools.Debug.Performance;

public class FpsLabelFeature(DebugOverlay overlay) : DebugFeature
{
    private FpsLabel? _label;

    public override string DisplayName => "FPS label";

    public override DebugGroup Group => DebugGroup.Performance;

    public override bool IsAvailable(Node? scene)
    {
        return true;
    }

    protected override void Enable(Node? scene)
    {
        _label = new FpsLabel();

        overlay.AddChild(_label);
    }

    protected override void Disable()
    {
        _label?.QueueFree();
        _label = null;
    }
}
