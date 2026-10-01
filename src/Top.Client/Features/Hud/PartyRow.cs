using System;
using System.Globalization;
using Godot;
using Top.Client.Features.Characters.Portrait;
using Top.Client.Features.Party;
using Top.Client.Rendering.Models;
using Top.Contracts.Tables.Characters;
using Top.Contracts.Tables.Items;
using Top.Contracts.Tables.Rigs;

namespace Top.Client.Features.Hud;

public partial class PartyRow : Control
{
    private const string HaloVariation = "HudPartyPortraitHalo";

    private const string LeaderHaloVariation = "HudPartyPortraitHaloLeader";

    private const string DeadHaloVariation = "HudPartyPortraitHaloDead";

    private const string DiscVariation = "HudPartyPortraitDisc";

    private const string LeaderDiscVariation = "HudPartyPortraitDiscLeader";

    private const string DeadDiscVariation = "HudPartyPortraitDiscDead";

    private const string NameVariation = "HudPartyName";

    private const string DeadNameVariation = "HudPartyNameDead";

    [Export] public Panel PortraitHalo { get; set; } = null!;

    [Export] public Panel PortraitDisc { get; set; } = null!;

    [Export] public CharacterPortrait Portrait { get; set; } = null!;

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Label LevelLabel { get; set; } = null!;

    [Export] public Label HealthLabel { get; set; } = null!;

    [Export] public Label DeadLabel { get; set; } = null!;

    [Export] public HealthBar HealthBar { get; set; } = null!;

    [Export] public HudBar ManaBar { get; set; } = null!;

    public void Setup(CharacterTable characterTable, PortraitCameraTable portraitCameraTable,
        ModelFactory modelFactory, RigTable rigTable, ActionSetTable actionSetTable, ItemTable itemTable)
    {
        Portrait.Setup(characterTable, portraitCameraTable, modelFactory, rigTable, actionSetTable, itemTable);
    }

    public void ShowMember(PartyRosterMember member)
    {
        var vitals = member.Vitals;
        var maxHealth = Math.Max(vitals?.MaxHealth ?? 0, 0);
        var health = Math.Clamp(vitals?.Health ?? 0, 0, maxHealth);
        var maxMana = Math.Max(vitals?.MaxMana ?? 0, 0);
        var mana = Math.Clamp(vitals?.Mana ?? 0, 0, maxMana);
        var isDead = vitals != null && health == 0;

        NameLabel.Text = member.Name;
        NameLabel.ThemeTypeVariation = isDead ? DeadNameVariation : NameVariation;

        LevelLabel.Visible = vitals != null;
        LevelLabel.Text = string.Create(CultureInfo.InvariantCulture, $"LV {vitals?.Level}");

        HealthLabel.Visible = vitals != null && !isDead;
        HealthLabel.Text = string.Create(CultureInfo.InvariantCulture, $"{health} / {maxHealth}");
        DeadLabel.Visible = isDead;

        PortraitHalo.ThemeTypeVariation = isDead ? DeadHaloVariation : member.IsLeader ? LeaderHaloVariation : HaloVariation;
        PortraitDisc.ThemeTypeVariation = isDead ? DeadDiscVariation : member.IsLeader ? LeaderDiscVariation : DiscVariation;

        Portrait.Visible = vitals != null;

        if (vitals != null)
        {
            Portrait.ShowCharacter(vitals.TypeId, vitals.Look);
            Portrait.SetDead(isDead);
        }

        HealthBar.UpdateAmount(health, maxHealth);
        ManaBar.UpdateAmount(isDead ? 0 : mana, maxMana);
    }
}
