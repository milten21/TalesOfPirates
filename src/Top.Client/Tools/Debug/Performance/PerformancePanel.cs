using Godot;

namespace Top.Client.Tools.Debug.Performance;

public partial class PerformancePanel : DebugWindow
{
    private const double RefreshIntervalSeconds = 0.25;

    private const double MicrosecondsPerMillisecond = 1000.0;

    private const double MillisecondsPerSecond = 1000.0;

    private readonly FrameTimeGraph _graph = new FrameTimeGraph();

    private readonly Label _framesPerSecond = new Label();

    private readonly Label _frameTime = new Label();

    private readonly Label _processTime = new Label();

    private readonly Label _cpuRenderTime = new Label();

    private readonly Label _gpuRenderTime = new Label();

    private readonly Label _drawCalls = new Label();

    private readonly Label _objects = new Label();

    private readonly Label _primitives = new Label();

    private Rid _measuredViewport;

    private ulong _lastFrameMicroseconds;

    private double _secondsSinceRefresh = RefreshIntervalSeconds;

    private double _frameMillisecondsSinceRefresh;

    private int _framesSinceRefresh;

    public PerformancePanel()
    {
        Name = nameof(PerformancePanel);
        Title = "Performance";

        var values = new GridContainer { Columns = 2 };

        AddValue(values, "FPS", _framesPerSecond);
        AddValue(values, "Frame time", _frameTime);
        AddValue(values, "Process time", _processTime);
        AddValue(values, "CPU render time", _cpuRenderTime);
        AddValue(values, "GPU render time", _gpuRenderTime);
        AddValue(values, "Draw calls", _drawCalls);
        AddValue(values, "Objects", _objects);
        AddValue(values, "Primitives", _primitives);

        _graph.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        var list = new VBoxContainer();

        list.AddChild(values);
        list.AddChild(_graph);

        AddContent(list);
    }

    public override void _EnterTree()
    {
        _measuredViewport = GetTree().Root.GetViewportRid();
        _lastFrameMicroseconds = Time.GetTicksUsec();
    }

    public override void _Process(double delta)
    {
        var now = Time.GetTicksUsec();
        var frameMilliseconds = (now - _lastFrameMicroseconds) / MicrosecondsPerMillisecond;

        _lastFrameMicroseconds = now;

        _graph.AddFrame((float)frameMilliseconds);

        _frameMillisecondsSinceRefresh += frameMilliseconds;
        _framesSinceRefresh++;
        _secondsSinceRefresh += frameMilliseconds / MillisecondsPerSecond;

        if (_secondsSinceRefresh < RefreshIntervalSeconds)
        {
            return;
        }

        ShowValues(_frameMillisecondsSinceRefresh / _framesSinceRefresh);

        _secondsSinceRefresh = 0;
        _frameMillisecondsSinceRefresh = 0;
        _framesSinceRefresh = 0;
    }

    private static void AddValue(GridContainer values, string name, Label value)
    {
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        values.AddChild(new Label { Text = name });
        values.AddChild(value);
    }

    private static string FormatMilliseconds(double milliseconds)
    {
        return $"{milliseconds:0.00} ms";
    }

    private static string FormatCount(Godot.Performance.Monitor monitor)
    {
        return $"{Godot.Performance.GetMonitor(monitor):N0}";
    }

    private void ShowValues(double averageFrameMilliseconds)
    {
        var processSeconds = Godot.Performance.GetMonitor(Godot.Performance.Monitor.TimeProcess);
        var cpuRenderMilliseconds = RenderingServer.ViewportGetMeasuredRenderTimeCpu(_measuredViewport)
                                    + RenderingServer.GetFrameSetupTimeCpu();
        var gpuRenderMilliseconds = RenderingServer.ViewportGetMeasuredRenderTimeGpu(_measuredViewport);

        _framesPerSecond.Text = $"{Engine.GetFramesPerSecond():0}";
        _frameTime.Text = FormatMilliseconds(averageFrameMilliseconds);
        _processTime.Text = FormatMilliseconds(processSeconds * MillisecondsPerSecond);
        _cpuRenderTime.Text = FormatMilliseconds(cpuRenderMilliseconds);
        _gpuRenderTime.Text = FormatMilliseconds(gpuRenderMilliseconds);
        _drawCalls.Text = FormatCount(Godot.Performance.Monitor.RenderTotalDrawCallsInFrame);
        _objects.Text = FormatCount(Godot.Performance.Monitor.RenderTotalObjectsInFrame);
        _primitives.Text = FormatCount(Godot.Performance.Monitor.RenderTotalPrimitivesInFrame);
    }
}
