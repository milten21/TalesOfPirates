using System;
using Godot;
using Top.Client.Messages;
using Top.Client.Tools.Debug.MapOverlays;
using Top.Client.Tools.Debug.Packets;
using Top.Client.Tools.Debug.Performance;
using Top.Client.Tools.Debug.ServerPaths;

namespace Top.Client.Tools.Debug;

public partial class DebugOverlay : CanvasLayer
{
    public static readonly StringName MenuAction = "debug_menu";

    private const int TopLayer = 128;

    private readonly DebugMenu _menu = new DebugMenu();

    private DebugFeature[] _features = [];

    private IServerConnection _connection = null!;

    public PacketLog PacketLog { get; } = new PacketLog();

    public void Setup(IServerConnection connection)
    {
        _connection = connection;
    }

    public override void _Ready()
    {
        Name = nameof(DebugOverlay);
        Layer = TopLayer;
        ProcessMode = ProcessModeEnum.Always;

        var serverPathRecorder = new ServerPathRecorder(_connection, TimeProvider.System);

        _features =
        [
            new FpsLabelFeature(this),
            new PerformancePanelFeature(this),
            new PacketLogFeature(this, PacketLog),
            new ServerPathFeature(this, serverPathRecorder, ServerPathScope.Player),
            new ServerPathFeature(this, serverPathRecorder, ServerPathScope.OtherCharacters),
            new MapOverlayFeature("Cell grid", (grid, _) => new CellGridMeshBuilder(grid)),
            new MapOverlayFeature("Cell height colour", (grid, _) => new CellHeightMeshBuilder(grid)),
            new MapOverlayFeature("Blocked cells", (grid, walkability) => new BlockedCellMeshBuilder(grid, walkability))
        ];

        _menu.Setup(_features);

        AddWindow(_menu);
    }

    public override void _UnhandledKeyInput(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed(MenuAction))
        {
            return;
        }

        GetViewport().SetInputAsHandled();

        ToggleMenu();
    }

    public override void _Process(double delta)
    {
        var scene = GetTree().CurrentScene;

        foreach (var feature in _features)
        {
            feature.Apply(scene);
        }

        if (_menu.Visible)
        {
            _menu.Refresh(scene);
        }
    }

    public void AddWindow(DebugWindow window)
    {
        window.MenuKeyPressed += ToggleMenu;

        AddChild(window);
    }

    private void ToggleMenu()
    {
        _menu.Visible = !_menu.Visible;
    }
}
