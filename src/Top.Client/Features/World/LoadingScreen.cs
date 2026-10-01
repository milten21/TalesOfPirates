using System;
using Godot;
using Top.Client.Features.Maps;

namespace Top.Client.Features.World;

public partial class LoadingScreen : Panel
{
    private const double PulseSeconds = 1.6;

    private const float PulseLowOpacity = 0.35f;

    private const double FadeSeconds = 0.3;

    private const double EaseRate = 1.2;

    private const double StageEndGap = 0.1;

    private const double FullProgress = 100;

    private const string ReturnText = "Returning to character select";

    private const string BarType = "LoadingBar";

    private const string FailedBarType = "LoadingBarFailed";

    private static readonly StageShare[] StageShares =
    [
        new StageShare(MapLoadStage.Tables, "Reading tables", 5),
        new StageShare(MapLoadStage.Materials, "Loading materials", 10),
        new StageShare(MapLoadStage.MapFile, "Reading map", 10),
        new StageShare(MapLoadStage.ModelCache, "Loading models", 60),
        new StageShare(MapLoadStage.TerrainAndWater, "Building world", 15)
    ];

    private Tween? _pulse;

    private Tween? _fade;

    private Tween? _destinationFade;

    private double _progress;

    private double _stageEnd;

    private bool _isRunning;

    [Export] public Control Diamond { get; set; } = null!;

    [Export] public ProgressBar Bar { get; set; } = null!;

    [Export] public Label StageWord { get; set; } = null!;

    [Export] public Label Percent { get; set; } = null!;

    [Export] public Label Title { get; set; } = null!;

    [Export] public Control ZoneLine { get; set; } = null!;

    [Export] public Label ZoneTypeWord { get; set; } = null!;

    [Export] public Label ZoneSeparator { get; set; } = null!;

    [Export] public Label ZoneRuleWord { get; set; } = null!;

    [Export] public Label Tip { get; set; } = null!;

    public override void _Process(double delta)
    {
        if (!_isRunning)
        {
            return;
        }

        var eased = _progress + ((_stageEnd - _progress) * (1 - Math.Exp(-EaseRate * delta)));

        ShowProgress(Math.Max(_progress, Math.Min(eased, _stageEnd - StageEndGap)));
    }

    public void Begin()
    {
        Open();

        _isRunning = true;

        Percent.Show();

        ShowStage(StageShares[0].Stage);
    }

    public void Return()
    {
        Open();

        StageWord.Text = ReturnText;
        Percent.Hide();
    }

    public void ShowStage(MapLoadStage stage)
    {
        var stageEnd = 0.0;

        foreach (var share in StageShares)
        {
            stageEnd += share.Share;

            if (share.Stage == stage)
            {
                StageWord.Text = share.Word;
                _stageEnd = stageEnd;

                return;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(stage), stage, "The loading screen has no share for this stage.");
    }

    public void ShowDestination(Destination destination)
    {
        Title.Text = destination.Title;

        ZoneTypeWord.Visible = destination.ZoneType != null;
        ZoneSeparator.Visible = destination.ZoneType != null;

        if (destination.ZoneType is { } type)
        {
            ShowZoneWord(ZoneTypeWord, ToZoneWord(type));
        }

        ShowZoneWord(ZoneRuleWord, ToZoneWord(destination.ZoneRule));

        _destinationFade?.Kill();
        _destinationFade = CreateTween().SetParallel();
        _destinationFade.TweenProperty(Title, "modulate:a", 1f, FadeSeconds);
        _destinationFade.TweenProperty(ZoneLine, "modulate:a", 1f, FadeSeconds);
    }

    public void ShowTip(string tip)
    {
        Tip.Text = tip;
        Tip.Show();
    }

    public void Finish()
    {
        _isRunning = false;

        ShowProgress(FullProgress);

        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", 0f, FadeSeconds);
        _fade.TweenCallback(Callable.From(Close));
    }

    public void Fail()
    {
        _isRunning = false;

        StopPulse();

        StageWord.Text = ReturnText;
        Percent.Hide();
        Bar.ThemeTypeVariation = FailedBarType;
    }

    private static ZoneWord ToZoneWord(ZoneType type)
    {
        return type switch
        {
            ZoneType.City => new ZoneWord("CITY", "LoadingZoneCity"),
            ZoneType.Field => new ZoneWord("FIELD", "LoadingZoneField"),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "The loading screen has no word for this type.")
        };
    }

    private static ZoneWord ToZoneWord(ZoneRule rule)
    {
        return rule switch
        {
            ZoneRule.Safe => new ZoneWord("SAFE ZONE", "LoadingZoneSafe"),
            ZoneRule.PlayerKilling => new ZoneWord("PK ZONE", "LoadingZoneBattle"),
            ZoneRule.Battle => new ZoneWord("BATTLE ZONE", "LoadingZoneBattle"),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "The loading screen has no word for this rule.")
        };
    }

    private static void ShowZoneWord(Label label, ZoneWord word)
    {
        label.Text = word.Text;
        label.ThemeTypeVariation = word.ThemeType;
    }

    private void Open()
    {
        _fade?.Kill();
        _destinationFade?.Kill();
        _isRunning = false;

        Title.Modulate = Colors.Transparent;
        ZoneLine.Modulate = Colors.Transparent;
        Tip.Hide();

        Modulate = Colors.White;
        Bar.ThemeTypeVariation = BarType;

        ShowProgress(0);
        StartPulse();
        Show();
    }

    private void ShowProgress(double progress)
    {
        _progress = progress;

        Bar.Value = progress;
        Percent.Text = $"{Math.Floor(progress)}%";
    }

    private void StartPulse()
    {
        StopPulse();

        _pulse = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _pulse.TweenProperty(Diamond, "modulate:a", PulseLowOpacity, PulseSeconds / 2);
        _pulse.TweenProperty(Diamond, "modulate:a", 1f, PulseSeconds / 2);
    }

    private void StopPulse()
    {
        _pulse?.Kill();
        _pulse = null;

        Diamond.Modulate = Colors.White;
    }

    private void Close()
    {
        StopPulse();
        Hide();
    }

    private record StageShare(MapLoadStage Stage, string Word, double Share);

    private record ZoneWord(string Text, string ThemeType);
}
