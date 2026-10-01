using System;
using Godot;
using Top.Client.Core;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Features.Characters;

public partial class Nameplate : Node3D
{
    private const int MonsterLevelGate = 10;

    private const float HalfHealth = 0.5f;

    private const byte HealthAlpha = 0xB3;

    private const string ShowAllNamesAction = "show_all_names";

    private const string ShowTeamNamesAction = "show_team_names";

    private Camera3D? _camera;

    private InputGate? _inputGate;

    private CharacterState? _character;

    private CharacterState? _player;

    private bool _showsName;

    private bool _showsHealth;

    private float _healthFraction;

    private float _pixelSize;

    [Export] public Label3D NameLabel { get; set; } = null!;

    [Export] public Label3D GuildLabel { get; set; } = null!;

    [Export] public Label3D HealthLabel { get; set; } = null!;

    [Export] public Sprite3D HealthBar { get; set; } = null!;

    [Export] public Sprite3D HealthFill { get; set; } = null!;

    [Export] public float Gap { get; set; } = 4f;

    private Vector2 BarSize => HealthBar.Texture.GetSize();

    public void Setup(Camera3D camera, InputGate inputGate, CharacterState character, CharacterState player)
    {
        _camera = camera;
        _inputGate = inputGate;
        _player = player;

        ChangeCharacter(character);
    }

    public void ChangeCharacter(CharacterState character)
    {
        if (_player == null)
        {
            throw new InvalidOperationException($"the nameplate of {character.Name} changed before its setup");
        }

        _character = character;

        // TODO: follow the player's level changes too; the monster level gate reads the player state of the setup or
        // of the last team leader change.
        NameLabel.Text = ToNameText(character, _player.Level);
        GuildLabel.Text = ToGuildText(character);

        var maxHealth = Mathf.Max(character.MaxHealth, 0);
        var health = Mathf.Clamp(character.Health, 0, maxHealth);
        _healthFraction = maxHealth > 0 ? (float)health / maxHealth : 0f;

        var color = MeasureHealthColor(_healthFraction);

        HealthLabel.Text = $"{health}/{maxHealth}";
        HealthLabel.Modulate = color;
        HealthFill.Modulate = color;
        HealthFill.RegionRect = new Rect2(0f, 0f, BarSize.X * _healthFraction, BarSize.Y);

        Arrange();
    }

    public void ChangePlayer(CharacterState player)
    {
        _player = player;

        if (_character != null)
        {
            ChangeCharacter(_character);
        }
    }

    public override void _Process(double delta)
    {
        if (_camera == null || _inputGate == null || _character == null || _player == null)
        {
            return;
        }

        var rule = new NameplateRule(_player, _inputGate.IsActionPressed(ShowAllNamesAction),
            _inputGate.IsActionPressed(ShowTeamNamesAction));

        ScaleTo(MeasurePixelSize(_camera));
        ShowParts(rule.ShowsName(_character), rule.ShowsHealth(_character));
    }

    private static float MeasurePixelSize(Camera3D camera)
    {
        var projection = camera.GetCameraProjection();

        return 2f / (projection.Y.Y * camera.GetViewport().GetVisibleRect().Size.Y);
    }

    private static string ToNameText(CharacterState character, int playerLevel)
    {
        if (character.ControlType != CharacterControlType.Monster)
        {
            return character.Name;
        }

        return character.Level - playerLevel <= MonsterLevelGate
            ? $"Lv{character.Level} {character.Name}"
            : $"??? {character.Name}";
    }

    private static string ToGuildText(CharacterState character)
    {
        if (character.GuildName.Length == 0)
        {
            return string.Empty;
        }

        // TODO: the retheme lane draws the guild leader flag, texture/ui/guildflag.tga, left of the guild line.
        return character.GuildMotto.Length > 0
            ? $"{character.GuildName}({character.GuildMotto})"
            : character.GuildName;
    }

    private static Color MeasureHealthColor(float fraction)
    {
        return fraction > HalfHealth
            ? Color.Color8((byte)(510 - (fraction * 510)), 255, 0, HealthAlpha)
            : Color.Color8(255, (byte)(fraction * 510), 0, HealthAlpha);
    }

    private static float MeasureLineHeight(Label3D label)
    {
        return label.Font.GetHeight(label.FontSize) + label.LineSpacing;
    }

    private void ScaleTo(float pixelSize)
    {
        if (pixelSize == _pixelSize)
        {
            return;
        }

        _pixelSize = pixelSize;
        Scale = new Vector3(pixelSize, pixelSize, pixelSize);
    }

    private void ShowParts(bool showsName, bool showsHealth)
    {
        if (showsName == _showsName && showsHealth == _showsHealth)
        {
            return;
        }

        _showsName = showsName;
        _showsHealth = showsHealth;

        Arrange();
    }

    private void Arrange()
    {
        var showsBar = _showsHealth && _healthFraction > 0f;
        var hasGuild = _showsName && GuildLabel.Text.Length > 0;

        HealthBar.Visible = showsBar;
        HealthFill.Visible = showsBar;
        HealthLabel.Visible = showsBar;
        GuildLabel.Visible = hasGuild;
        NameLabel.Visible = _showsName;

        var bottom = Gap;

        if (showsBar)
        {
            HealthBar.Offset = new Vector2(-BarSize.X / 2f, bottom);
            HealthFill.Offset = HealthBar.Offset;
            bottom += BarSize.Y;

            HealthLabel.Offset = new Vector2(0f, bottom);
            bottom += MeasureLineHeight(HealthLabel);
        }

        if (hasGuild)
        {
            GuildLabel.Offset = new Vector2(0f, bottom);
            bottom += MeasureLineHeight(GuildLabel);
        }

        // TODO: the retheme lane draws the team leader flag, the original's GetLeaderPic, over a leader's name.
        NameLabel.Offset = new Vector2(0f, bottom);
    }
}
