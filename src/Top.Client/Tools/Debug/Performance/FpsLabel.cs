using Godot;

namespace Top.Client.Tools.Debug.Performance;

public partial class FpsLabel : Label
{
    private const double RefreshIntervalSeconds = 0.25;

    private const int Margin = 8;

    private double _secondsSinceRefresh = RefreshIntervalSeconds;

    public FpsLabel()
    {
        Name = nameof(FpsLabel);
        ThemeTypeVariation = "FpsLabel";
        HorizontalAlignment = HorizontalAlignment.Right;
        MouseFilter = MouseFilterEnum.Ignore;

        SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight, LayoutPresetMode.KeepSize, Margin);
        GrowHorizontal = GrowDirection.Begin;
    }

    public override void _Process(double delta)
    {
        _secondsSinceRefresh += delta;

        if (_secondsSinceRefresh < RefreshIntervalSeconds)
        {
            return;
        }

        _secondsSinceRefresh = 0;

        Text = $"{Engine.GetFramesPerSecond():0} FPS";
    }
}
