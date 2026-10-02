using Godot;

namespace Top.Client.Tools.Debug.Performance;

public class PerformancePanelFeature(DebugOverlay overlay) : DebugFeature
{
    private const int SideMargin = 24;

    private const int TopMargin = 64;

    private static readonly Vector2I PanelSize = new Vector2I(300, 340);

    private PerformancePanel? _panel;

    public override string DisplayName => "Performance panel";

    public override DebugGroup Group => DebugGroup.Performance;

    public override bool IsAvailable(Node? scene)
    {
        return true;
    }

    protected override void Enable(Node? scene)
    {
        var viewport = overlay.GetViewport();

        RenderingServer.ViewportSetMeasureRenderTime(viewport.GetViewportRid(), true);

        var visibleWidth = (int)viewport.GetVisibleRect().Size.X;

        _panel = new PerformancePanel
        {
            Position = new Vector2I(visibleWidth - PanelSize.X - SideMargin, TopMargin),
            Size = PanelSize
        };
        _panel.CloseRequested += TurnOff;

        overlay.AddWindow(_panel);
    }

    protected override void Disable()
    {
        RenderingServer.ViewportSetMeasureRenderTime(overlay.GetViewport().GetViewportRid(), false);

        _panel?.QueueFree();
        _panel = null;
    }

    private void TurnOff()
    {
        IsOn = false;
    }
}
