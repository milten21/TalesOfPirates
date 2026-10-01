using System;
using System.Globalization;
using Godot;
using Top.Client.Features.Characters.Portrait;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;

namespace Top.Client.Features.Hud;

public partial class PlayerFrame : Control
{
    private const string HaloVariation = "HudPortraitHalo";

    private const string LeaderHaloVariation = "HudPortraitHaloLeader";

    private const string DeadHaloVariation = "HudPortraitHaloDead";

    private const string DiscVariation = "HudPortraitDisc";

    private const string LeaderDiscVariation = "HudPortraitDiscLeader";

    private const string DeadDiscVariation = "HudPortraitDiscDead";

    private const string NameVariation = "HudPlayerName";

    private const string DeadNameVariation = "HudPlayerNameDead";

    private const string MaximumLevelText = "MAX";

    private bool _isDead;

    private bool _isLeader;

    [Export] public Panel PortraitHalo { get; set; } = null!;

    [Export] public Panel PortraitDisc { get; set; } = null!;

    [Export] public CharacterPortrait Portrait { get; set; } = null!;

    [Export] public Label LevelLabel { get; set; } = null!;

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Label JobLabel { get; set; } = null!;

    [Export] public Control HealthNumbers { get; set; } = null!;

    [Export] public Label HealthLabel { get; set; } = null!;

    [Export] public Label MaxHealthLabel { get; set; } = null!;

    [Export] public Label DeadLabel { get; set; } = null!;

    [Export] public HealthBar HealthBar { get; set; } = null!;

    [Export] public HudBar ManaBar { get; set; } = null!;

    [Export] public HudBar ExperienceLine { get; set; } = null!;

    [Export] public Label ExperienceLabel { get; set; } = null!;

    public void Setup(CharacterTable characterTable, PortraitCameraTable portraitCameraTable,
        ModelFactory modelFactory, RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable)
    {
        Portrait.Setup(characterTable, portraitCameraTable, modelFactory, rigTable, actionSetTable, itemTable);
    }

    public void ShowCharacter(CharacterState character, JobTable jobs)
    {
        var maxHealth = Math.Max(character.MaxHealth, 0);
        var health = Math.Clamp(character.Health, 0, maxHealth);
        var maxMana = Math.Max(character.MaxMana, 0);
        var mana = Math.Clamp(character.Mana, 0, maxMana);
        var isDead = health == 0;

        LevelLabel.Text = character.Level.ToString(CultureInfo.InvariantCulture);
        NameLabel.Text = character.Name;
        JobLabel.Text = jobs.NameOf(character.Job).ToUpperInvariant();

        NameLabel.ThemeTypeVariation = isDead ? DeadNameVariation : NameVariation;

        _isDead = isDead;
        ShowRing();

        Portrait.ShowCharacter(character.TypeId, character.Look);
        Portrait.SetDead(isDead);

        HealthNumbers.Visible = !isDead;
        DeadLabel.Visible = isDead;
        HealthLabel.Text = health.ToString(CultureInfo.InvariantCulture);
        MaxHealthLabel.Text = $" / {maxHealth.ToString(CultureInfo.InvariantCulture)}";

        HealthBar.UpdateAmount(health, maxHealth);
        ManaBar.UpdateAmount(isDead ? 0 : mana, maxMana);

        ShowExperience(character.Experience, character.LevelThreshold, character.NextLevelThreshold);
    }

    public void ShowLeader(bool isLeader)
    {
        _isLeader = isLeader;
        ShowRing();
    }

    private void ShowRing()
    {
        PortraitHalo.ThemeTypeVariation = _isDead ? DeadHaloVariation : _isLeader ? LeaderHaloVariation : HaloVariation;
        PortraitDisc.ThemeTypeVariation = _isDead ? DeadDiscVariation : _isLeader ? LeaderDiscVariation : DiscVariation;
    }

    private void ShowExperience(long experience, long levelThreshold, long nextLevelThreshold)
    {
        var span = nextLevelThreshold - levelThreshold;

        if (span <= 0)
        {
            ExperienceLine.Fraction = 1f;
            ExperienceLabel.Text = MaximumLevelText;

            return;
        }

        var fraction = Math.Clamp((double)(experience - levelThreshold) / span, 0.0, 1.0);
        var percent = Math.Floor(fraction * 10000.0) / 100.0;

        ExperienceLine.Fraction = (float)fraction;
        ExperienceLabel.Text = string.Create(CultureInfo.InvariantCulture, $"{percent:0.00}%");
    }
}
