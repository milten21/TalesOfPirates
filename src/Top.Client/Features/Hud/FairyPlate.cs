using System.Globalization;
using Godot;
using Top.Client.Messages.Play.Data;
using Top.Client.Rendering.Textures;
using Top.Contracts.Tables.Items;

namespace Top.Client.Features.Hud;

public partial class FairyPlate : Control
{
    private const double BlinkSeconds = 0.55;

    private const float DimmedOverlayOpacity = 0.35f;

    private const string OrbGlowVariation = "HudFairyOrbGlow";

    private const string GrowthFullOrbGlowVariation = "HudFairyOrbGlowGrowthFull";

    private const string GrowthBarVariation = "HudFairyGrowthBar";

    private const string GrowthFullBarVariation = "HudFairyGrowthBarGrowthFull";

    private double _blinkElapsed;

    private bool _isBlinkLit;

    private bool _isGrowthFull;

    private ItemTable _itemTable = null!;

    private TextureCache _textureCache = null!;

    private string? _iconPath;

    [Export] public Panel OrbGlow { get; set; } = null!;

    [Export] public TextureRect Icon { get; set; } = null!;

    [Export] public Control GrowthFullPlate { get; set; } = null!;

    [Export] public HudBar StaminaBar { get; set; } = null!;

    [Export] public HudBar GrowthBar { get; set; } = null!;

    [Export] public Label LevelLabel { get; set; } = null!;

    [Export] public Control Tip { get; set; } = null!;

    [Export] public Label TipHeader { get; set; } = null!;

    public override void _Ready()
    {
        Tip.Hide();

        MouseEntered += Tip.Show;
        MouseExited += Tip.Hide;

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _blinkElapsed += delta;

        if (_blinkElapsed < BlinkSeconds)
        {
            return;
        }

        _blinkElapsed -= BlinkSeconds;
        _isBlinkLit = !_isBlinkLit;

        ShowBlink();
    }

    public void Setup(ItemTable itemTable, TextureCache textureCache)
    {
        _itemTable = itemTable;
        _textureCache = textureCache;
    }

    public void ShowFairy(Fairy? fairy)
    {
        Visible = fairy != null;

        if (fairy == null)
        {
            Tip.Hide();
            SetGrowthFull(false);

            return;
        }

        var levelText = string.Create(CultureInfo.InvariantCulture, $"LV {fairy.Level}");

        LevelLabel.Text = levelText;
        TipHeader.Text = FindItem(fairy.ItemId)?.Name is { Length: > 0 } name ? $"{name} · {levelText}" : levelText;

        UpdateIcon(FindItem(fairy.AppearanceItemId ?? fairy.ItemId)?.IconPath);

        StaminaBar.UpdateAmount(fairy.Stamina, fairy.MaxStamina);
        GrowthBar.UpdateAmount(fairy.Growth, fairy.MaxGrowth);

        SetGrowthFull(fairy.MaxGrowth > 0 && fairy.Growth >= fairy.MaxGrowth);
    }

    private ItemEntry? FindItem(int itemId)
    {
        return _itemTable.TryGetById(itemId, out var item) ? item : null;
    }

    private async void UpdateIcon(string? iconPath)
    {
        if (iconPath == _iconPath)
        {
            return;
        }

        _iconPath = iconPath;

        if (iconPath == null)
        {
            SetTexture(null);

            return;
        }

        var texture = await _textureCache.Read(iconPath);

        if (iconPath == _iconPath && IsInstanceValid(this))
        {
            SetTexture(texture);
        }
    }

    private void SetTexture(Texture2D? texture)
    {
        Icon.Texture = texture;
        Icon.Visible = texture != null;
    }

    private void SetGrowthFull(bool isFull)
    {
        if (isFull == _isGrowthFull)
        {
            return;
        }

        _isGrowthFull = isFull;
        _isBlinkLit = false;
        _blinkElapsed = 0.0;

        OrbGlow.ThemeTypeVariation = isFull ? GrowthFullOrbGlowVariation : OrbGlowVariation;
        GrowthFullPlate.Visible = isFull;

        SetProcess(isFull);
        ShowBlink();
    }

    private void ShowBlink()
    {
        GrowthBar.ThemeTypeVariation = _isBlinkLit ? GrowthFullBarVariation : GrowthBarVariation;
        GrowthFullPlate.Modulate = new Color(1f, 1f, 1f, _isBlinkLit ? 1f : DimmedOverlayOpacity);
    }
}
