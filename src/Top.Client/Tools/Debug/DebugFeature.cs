using Godot;

namespace Top.Client.Tools.Debug;

public abstract class DebugFeature
{
    public abstract string DisplayName { get; }

    public abstract DebugGroup Group { get; }

    public bool IsOn { get; set; }

    public bool IsActive { get; private set; }

    public abstract bool IsAvailable(Node? scene);

    public void Apply(Node? scene)
    {
        var shouldBeActive = IsOn && IsAvailable(scene);

        if (shouldBeActive == IsActive)
        {
            return;
        }

        if (shouldBeActive)
        {
            Enable(scene);
        }
        else
        {
            Disable();
        }

        IsActive = shouldBeActive;
    }

    protected abstract void Enable(Node? scene);

    protected abstract void Disable();
}
