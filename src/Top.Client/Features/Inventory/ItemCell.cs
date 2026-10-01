using System.Globalization;
using Godot;
using Top.Client.Rendering.Textures;
using Top.Contracts.Tables.Items;

namespace Top.Client.Features.Inventory;

public partial class ItemCell : Panel
{
    private const float DragPreviewAlpha = 0.85f;

    private static readonly StringName HatchColorName = "hatch_color";

    private static readonly StringName GlyphColorName = "glyph_color";

    private static readonly StringName HatchWidthName = "hatch_width";

    private static readonly StringName HatchPeriodName = "hatch_period";

    private static readonly StringName ExpiredIconColorName = "expired_icon_color";

    private static readonly StringName DraggedColorName = "dragged_color";

    private static readonly StringName DimmedColorName = "dimmed_color";

    private static readonly StringName MarkGreenColorName = "mark_green_color";

    private static readonly StringName MarkPurpleColorName = "mark_purple_color";

    private static readonly StringName MarkRedOrangeColorName = "mark_red_orange_color";

    private static readonly StringName MarkGoldColorName = "mark_gold_color";

    private static readonly StringName MarkBorderName = "mark_border";

    private static readonly StringName MarkGlowName = "mark_glow";

    private static readonly Vector2 HatchAcross = new Vector2(1f, 1f).Normalized();

    private static readonly Vector2 HatchAlong = new Vector2(1f, -1f).Normalized();

    private TextureCache _textureCache = null!;

    private string? _iconPath;

    private bool _isExpired;

    private bool _isLocked;

    private float _cooldownLeft;

    private Color? _markColor;

    private StyleBox? _markBorder;

    private StyleBox? _markGlow;

    [Export] public TextureRect Glyph { get; set; } = null!;

    [Export] public TextureRect Icon { get; set; } = null!;

    [Export] public Control CooldownFill { get; set; } = null!;

    [Export] public Label Count { get; set; } = null!;

    [Export] public Control DropRing { get; set; } = null!;

    public override void _Ready()
    {
        Glyph.SelfModulate = GetThemeColor(GlyphColorName);
    }

    public void Setup(TextureCache textureCache)
    {
        _textureCache = textureCache;
    }

    public void ShowGlyph(Texture2D? glyph)
    {
        Glyph.Texture = glyph;
    }

    public void ShowItem(ItemEntry? item, int count, bool isExpired)
    {
        Count.Text = count > 1 ? count.ToString(CultureInfo.InvariantCulture) : string.Empty;
        Glyph.Visible = item == null;

        UpdateIcon(item?.IconPath, isExpired && item != null);
    }

    public Control BuildDragPreview()
    {
        var preview = new Control();
        var icon = new TextureRect
        {
            Texture = Icon.Texture,
            Size = Icon.Size,
            Position = -Icon.Size / 2f,
            ExpandMode = Icon.ExpandMode,
            StretchMode = Icon.StretchMode,
            Modulate = new Color(Colors.White, DragPreviewAlpha),
        };

        preview.AddChild(icon);

        return preview;
    }

    public void ShowMark(ItemNameTone tone, int gemColor)
    {
        var color = MarkColorOf(tone, gemColor);

        if (color == _markColor)
        {
            return;
        }

        _markColor = color;
        _markBorder = color is { } border ? TintStyle(MarkBorderName, border) : null;
        _markGlow = color is { } glow ? TintStyle(MarkGlowName, glow) : null;

        QueueRedraw();
    }

    public void ShowDragged(bool isDragged)
    {
        var color = isDragged ? GetThemeColor(DraggedColorName) : Colors.White;

        Icon.Modulate = color;
        Count.Modulate = color;
    }

    public void ShowDropRing(bool isShown)
    {
        DropRing.Visible = isShown;

        if (isShown)
        {
            DropRing.GlobalPosition = GlobalPosition;
            DropRing.Size = Size;
        }
    }

    public void ShowDimmed(bool isDimmed)
    {
        Modulate = isDimmed ? GetThemeColor(DimmedColorName) : Colors.White;
    }

    public void ShowCooldown(float cooldownLeft)
    {
        if (cooldownLeft == _cooldownLeft)
        {
            return;
        }

        var inset = -CooldownFill.OffsetBottom;

        _cooldownLeft = cooldownLeft;
        CooldownFill.Visible = cooldownLeft > 0f;
        CooldownFill.OffsetTop = -inset - ((Size.Y - (2f * inset)) * cooldownLeft);
    }

    public void ShowLocked(bool isLocked)
    {
        if (isLocked == _isLocked)
        {
            return;
        }

        _isLocked = isLocked;

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_isLocked)
        {
            DrawHatch();
        }

        if (_markGlow != null && _markBorder != null)
        {
            var rect = new Rect2(Vector2.Zero, Size);

            DrawStyleBox(_markGlow, rect);
            DrawStyleBox(_markBorder, rect);
        }
    }

    private void DrawHatch()
    {
        var color = GetThemeColor(HatchColorName);
        var width = GetThemeConstant(HatchWidthName);
        var period = GetThemeConstant(HatchPeriodName);
        var reach = (Size.X + Size.Y) / Mathf.Sqrt2;
        var start = Size / 2f - HatchAcross * (reach / 2f);

        for (var offset = width / 2f; offset < reach; offset += period)
        {
            var centre = start + HatchAcross * offset;

            DrawLine(centre - HatchAlong * reach, centre + HatchAlong * reach, color, width, true);
        }
    }

    private Color? MarkColorOf(ItemNameTone tone, int gemColor)
    {
        return tone switch
        {
            ItemNameTone.Green => GetThemeColor(MarkGreenColorName),
            ItemNameTone.Purple => GetThemeColor(MarkPurpleColorName),
            ItemNameTone.RedOrange => GetThemeColor(MarkRedOrangeColorName),
            ItemNameTone.Gold => GetThemeColor(MarkGoldColorName),
            ItemNameTone.Gem => GemColors.ToColor(gemColor),
            _ => null,
        };
    }

    private StyleBoxFlat TintStyle(StringName name, Color color)
    {
        var style = (StyleBoxFlat)GetThemeStylebox(name).Duplicate();

        style.BorderColor *= color;
        style.BgColor *= color;

        return style;
    }

    private static Texture2D ToGreyscale(Texture2D texture)
    {
        var image = texture.GetImage();

        if (image.IsCompressed())
        {
            image.Decompress();
        }

        image.Convert(Image.Format.La8);
        image.Convert(Image.Format.Rgba8);

        return ImageTexture.CreateFromImage(image);
    }

    private async void UpdateIcon(string? iconPath, bool isExpired)
    {
        if (iconPath == _iconPath && isExpired == _isExpired)
        {
            return;
        }

        _iconPath = iconPath;
        _isExpired = isExpired;
        Icon.SelfModulate = isExpired ? GetThemeColor(ExpiredIconColorName) : Colors.White;

        if (iconPath == null)
        {
            Icon.Texture = null;

            return;
        }

        var texture = await _textureCache.Read(iconPath);

        if (iconPath == _iconPath && isExpired == _isExpired && IsInstanceValid(this))
        {
            Icon.Texture = isExpired && texture != null ? ToGreyscale(texture) : texture;
        }
    }
}
