using Godot;

namespace Top.Client.Tools.Debug.Packets;

public class PacketLogFeature(DebugOverlay overlay, PacketLog log) : DebugFeature
{
    private static readonly Vector2I WindowPosition = new Vector2I(320, 64);

    private static readonly Vector2I WindowSize = new Vector2I(760, 560);

    private PacketLogWindow? _window;

    public override string DisplayName => "Packet log";

    public override DebugGroup Group => DebugGroup.Network;

    public override bool IsAvailable(Node? scene)
    {
        return true;
    }

    protected override void Enable(Node? scene)
    {
        log.Start();

        _window = PacketLogWindow.Create(log);
        _window.Position = WindowPosition;
        _window.Size = WindowSize;
        _window.CloseRequested += TurnOff;

        overlay.AddWindow(_window);
    }

    protected override void Disable()
    {
        log.Stop();

        _window?.QueueFree();
        _window = null;
    }

    private void TurnOff()
    {
        IsOn = false;
    }
}
