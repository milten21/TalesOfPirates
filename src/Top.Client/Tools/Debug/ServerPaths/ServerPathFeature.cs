using Godot;
using Top.Client.Features.World;

namespace Top.Client.Tools.Debug.ServerPaths;

public class ServerPathFeature(DebugOverlay overlay, ServerPathRecorder recorder, ServerPathScope scope)
    : DebugFeature
{
    private ServerPathView? _view;

    public override string DisplayName => scope == ServerPathScope.Player
        ? "Server path: player"
        : "Server path: others";

    public override DebugGroup Group => DebugGroup.Characters;

    public override bool IsAvailable(Node? scene)
    {
        return scene is WorldScene;
    }

    protected override void Enable(Node? scene)
    {
        recorder.Start();

        _view = ServerPathView.Create(recorder, scope);

        overlay.AddChild(_view);
    }

    protected override void Disable()
    {
        recorder.Stop();

        _view?.QueueFree();
        _view = null;
    }
}
