using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Top.Client.Features.Inventory.Tooltip;

public partial class ItemTooltip : PanelContainer
{
    private const string ThemeType = "ItemTooltip";

    private const string TextVariation = "ItemTooltipText";

    private const string DescriptionVariation = "ItemTooltipDescription";

    private const string KeyVariation = "ItemTooltipKey";

    private const string StrongVariation = "ItemTooltipStrong";

    private const string LockedVariation = "ItemTooltipLocked";

    private const string RestrictionVariation = "ItemTooltipRestriction";

    private const string RestrictionLabelVariation = "ItemTooltipRestrictionLabel";

    private const string FooterVariation = "ItemTooltipFooter";

    private const string FitsVariation = "ItemTooltipFits";

    private const string FitVariation = "ItemTooltipFit";

    private const string FitLabelVariation = "ItemTooltipFitLabel";

    private const string GemLevelVariation = "ItemTooltipGemLevel";

    private const string CurrentGemLevelVariation = "ItemTooltipGemLevelCurrent";

    private const float LineGap = 2f;

    private const float SectionGap = 8f;

    private const float FirstRowGap = 6f;

    private const float NoteGap = 6f;

    private const float RowSeparation = 20f;

    private const float FooterSeparation = 24f;

    private const float SocketSeparation = 8f;

    private const float SocketSize = 11f;

    private const int SocketOutlineWidth = 2;

    private const float RestrictionSeparation = 4f;

    private const float FitSeparation = 6f;

    private const float FitLineSeparation = 4f;

    private const float GemLadderGap = 6f;

    private const float CurrentGemLevelShare = 0.22f;

    private const int GemLevelCornerRadius = 3;

    private const float GemLevelOverhang = 6f;

    private const float GemLevelPadding = 1f;

    private const string LockedSlotName = "Locked slot";

    private const string LockedSlotTypeLine = "Bag";

    private const string LockedSlotText = "Use an inventory expansion item to unlock 4 more slots.";

    private static readonly (ItemRestrictions Restriction, string Text)[] RestrictionTexts =
    [
        (ItemRestrictions.Trade, "Can't trade"),
        (ItemRestrictions.Throw, "Can't throw"),
        (ItemRestrictions.Delete, "Can't delete"),
    ];

    private readonly List<Control> _sections = [];

    [Export] public Container Lines { get; set; } = null!;

    [Export] public Control EquippedChip { get; set; } = null!;

    [Export] public Label EquippedLabel { get; set; } = null!;

    [Export] public Label NameLabel { get; set; } = null!;

    [Export] public Control LevelChip { get; set; } = null!;

    [Export] public Label LevelLabel { get; set; } = null!;

    [Export] public Label TypeLine { get; set; } = null!;

    public void ShowItem(ItemDescription description)
    {
        EquippedChip.Hide();
        ShowDescription(description);
    }

    public void ShowEquippedItem(ItemDescription description, string slotName)
    {
        EquippedLabel.Text = $"Equipped · {slotName}";
        EquippedChip.Show();
        ShowDescription(description);
    }

    public void ShowLockedSlot()
    {
        ClearSections();

        EquippedChip.Hide();
        NameLabel.Text = LockedSlotName;
        NameLabel.AddThemeColorOverride("font_color", GetThemeColor("name_white_color"));
        LevelChip.Hide();
        ShowTypeLine(LockedSlotTypeLine);
        AddText(LockedSlotText);
        Show();
    }

    private void ShowDescription(ItemDescription description)
    {
        ClearSections();

        NameLabel.Text = description.Name;
        NameLabel.AddThemeColorOverride("font_color", NameColorOf(description));
        LevelChip.Visible = description.Level != null;
        LevelLabel.Text = string.Create(CultureInfo.InvariantCulture, $"Lv {description.Level}");
        ShowTypeLine(description.TypeLine);

        if (description.Text != null)
        {
            AddText(description.Text);
        }

        AddFits(description.Fits);
        AddGemLevels(description.GemLevels, NameColorOf(description), description.Fits.Count > 0);
        AddRows(description.Rows);
        AddSockets(description.Sockets);
        AddRestrictions(description.Restrictions);
        AddFooter(description);

        if (description.Expiry != null)
        {
            AddGap(SectionGap);
            AddLabel(description.Expiry.Text, StrongVariation, ExpiryColorOf(description.Expiry.Stage));
        }

        if (description.IsLocked)
        {
            AddGap(NoteGap);
            AddLabel("Locked", LockedVariation);
        }

        Show();
    }

    private Color GetThemeColor(string name)
    {
        return GetThemeColor(name, ThemeType);
    }

    private Color NameColorOf(ItemDescription description)
    {
        return description.NameTone switch
        {
            ItemNameTone.Grey => GetThemeColor("name_grey_color"),
            ItemNameTone.Green => GetThemeColor("name_green_color"),
            ItemNameTone.Purple => GetThemeColor("name_purple_color"),
            ItemNameTone.RedOrange => GetThemeColor("name_red_orange_color"),
            ItemNameTone.Gold => GetThemeColor("name_gold_color"),
            ItemNameTone.Blue => GetThemeColor("name_blue_color"),
            ItemNameTone.Gem => GemColors.ToColor(description.GemColor),
            _ => GetThemeColor("name_white_color"),
        };
    }

    private Color RowColorOf(ItemRowKind kind)
    {
        return kind switch
        {
            ItemRowKind.WornStat => GetThemeColor("worn_stat_color"),
            ItemRowKind.Requirement => GetThemeColor("requirement_color"),
            ItemRowKind.UnmetRequirement => GetThemeColor("unmet_requirement_color"),
            ItemRowKind.Bonus => GetThemeColor("bonus_color"),
            ItemRowKind.Stamina => GetThemeColor("stamina_color"),
            ItemRowKind.Growth => GetThemeColor("growth_color"),
            ItemRowKind.FullGrowth => GetThemeColor("full_growth_color"),
            _ => GetThemeColor("stat_color"),
        };
    }

    private Color ExpiryColorOf(ItemExpiryStage stage)
    {
        return stage switch
        {
            ItemExpiryStage.LastDay => GetThemeColor("last_day_color"),
            ItemExpiryStage.Expired => GetThemeColor("expired_color"),
            _ => GetThemeColor("running_color"),
        };
    }

    private static ItemRowGroup GroupOf(ItemRowKind kind)
    {
        return kind switch
        {
            ItemRowKind.Requirement or ItemRowKind.UnmetRequirement => ItemRowGroup.Requirements,
            ItemRowKind.Bonus => ItemRowGroup.Bonuses,
            _ => ItemRowGroup.Stats,
        };
    }

    private void ShowTypeLine(string? typeLine)
    {
        TypeLine.Text = typeLine ?? string.Empty;
        TypeLine.Visible = typeLine != null;
    }

    private void AddText(string text)
    {
        AddGap(NoteGap);

        var label = AddLabel(text, DescriptionVariation);
        var width = GetThemeConstant("text_width", ThemeType);
        var font = label.GetThemeFont("font");
        var fontSize = label.GetThemeFontSize("font_size");

        if (font.GetStringSize(text, fontSize: fontSize).X > width)
        {
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            label.CustomMinimumSize = new Vector2(width, 0f);
        }
    }

    private void AddFits(IReadOnlyList<string> fits)
    {
        if (fits.Count == 0)
        {
            return;
        }

        AddGap(SectionGap);

        var width = GetThemeConstant("text_width", ThemeType);
        var chipMargin = GetThemeStylebox("panel", FitVariation).GetMinimumSize().X;
        var line = AddFitLine();
        var lineWidth = MeasureText("Fits", FitsVariation);

        line.AddChild(BuildCentredLabel("Fits", FitsVariation));

        foreach (var fit in fits)
        {
            var chipWidth = MeasureText(fit, FitLabelVariation) + chipMargin;

            if (lineWidth + FitSeparation + chipWidth > width)
            {
                AddGap(FitLineSeparation + LineGap * 2f);
                line = AddFitLine();
                lineWidth = chipWidth;
            }
            else
            {
                lineWidth += FitSeparation + chipWidth;
            }

            var chip = new PanelContainer
            {
                ThemeTypeVariation = FitVariation,
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore,
            };

            chip.AddChild(BuildLabel(fit, FitLabelVariation, null));
            line.AddChild(chip);
        }
    }

    private HBoxContainer AddFitLine()
    {
        var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };

        line.AddThemeConstantOverride("separation", (int)FitSeparation);
        AddSection(line);

        return line;
    }

    private static Label BuildCentredLabel(string text, string variation)
    {
        var label = BuildLabel(text, variation, null);

        label.SizeFlagsVertical = SizeFlags.ShrinkCenter;

        return label;
    }

    private float MeasureText(string text, string variation)
    {
        return GetThemeFont("font", variation).GetStringSize(text, fontSize: GetThemeFontSize("font_size", variation))
            .X;
    }

    private void AddGemLevels(IReadOnlyList<ItemGemLevel> gemLevels, Color gemColor, bool isAfterFits)
    {
        if (gemLevels.Count == 0)
        {
            return;
        }

        AddGap(isAfterFits ? GemLadderGap : SectionGap);

        foreach (var gemLevel in gemLevels)
        {
            var style = new StyleBoxFlat
            {
                BgColor = gemColor with { A = CurrentGemLevelShare },
                DrawCenter = gemLevel.IsCurrent,
                ContentMarginTop = GemLevelPadding,
                ContentMarginBottom = GemLevelPadding,
                ExpandMarginLeft = GemLevelOverhang,
                ExpandMarginRight = GemLevelOverhang,
            };
            var row = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            var variation = gemLevel.IsCurrent ? CurrentGemLevelVariation : GemLevelVariation;

            style.SetCornerRadiusAll(GemLevelCornerRadius);
            row.AddThemeStyleboxOverride("panel", style);
            row.AddChild(BuildPair(string.Create(CultureInfo.InvariantCulture, $"Lv {gemLevel.Level}"),
                gemLevel.Bonus, null, null, variation, RowSeparation, variation));
            AddSection(row);
        }
    }

    private void AddRows(IReadOnlyList<ItemRow> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            if (index == 0)
            {
                AddGap(FirstRowGap);
            }
            else if (GroupOf(rows[index].Kind) != GroupOf(rows[index - 1].Kind))
            {
                AddGap(SectionGap);
            }

            var color = RowColorOf(rows[index].Kind);

            AddSection(BuildPair(rows[index].Key, rows[index].Value, color, color, TextVariation, RowSeparation));
        }
    }

    private void AddSockets(IReadOnlyList<ItemSocket?> sockets)
    {
        if (sockets.Count == 0)
        {
            return;
        }

        AddGap(SectionGap);

        foreach (var socket in sockets)
        {
            var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var gemColor = socket != null ? GemColors.ToColor(socket.Color) : Colors.Transparent;

            row.AddThemeConstantOverride("separation", (int)SocketSeparation);
            row.AddChild(BuildSocketDot(gemColor, socket == null));
            row.AddChild(BuildLabel(socket?.Name ?? "Empty socket", TextVariation,
                socket != null ? gemColor : GetThemeColor("empty_socket_color"), isExpanding: true));

            if (socket?.Bonus != null)
            {
                row.AddChild(BuildLabel(socket.Bonus, TextVariation, GetThemeColor("gem_bonus_color")));
            }

            AddSection(row);
        }
    }

    private Control BuildSocketDot(Color gemColor, bool isEmpty)
    {
        var outlineColor = GetThemeColor("socket_outline_color");
        var dot = new Control
        {
            CustomMinimumSize = new Vector2(SocketSize, SocketSize),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        dot.Draw += () =>
        {
            var centre = dot.Size / 2f;

            if (isEmpty)
            {
                dot.DrawCircle(centre, (SocketSize - SocketOutlineWidth) / 2f, outlineColor, false, SocketOutlineWidth,
                    true);
            }
            else
            {
                dot.DrawCircle(centre, SocketSize / 2f, gemColor, true, -1f, true);
            }
        };

        return dot;
    }

    private void AddRestrictions(ItemRestrictions restrictions)
    {
        if (restrictions == ItemRestrictions.None)
        {
            return;
        }

        AddGap(SectionGap);

        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };

        row.AddThemeConstantOverride("separation", (int)RestrictionSeparation);

        foreach (var (restriction, text) in RestrictionTexts)
        {
            if (restrictions.HasFlag(restriction))
            {
                var chip = new PanelContainer
                {
                    ThemeTypeVariation = RestrictionVariation, MouseFilter = MouseFilterEnum.Ignore,
                };

                chip.AddChild(BuildLabel(text, RestrictionLabelVariation, null));
                row.AddChild(chip);
            }
        }

        AddSection(row);
    }

    private void AddFooter(ItemDescription description)
    {
        var rows = new List<(string Key, string Value)>();

        if (description.Stack != null)
        {
            rows.Add(("Stack", description.Stack));
        }

        if (description.TradeValue != null)
        {
            rows.Add(("Trade value", description.TradeValue.Value.ToString("N0", CultureInfo.InvariantCulture)));
        }

        if (rows.Count == 0)
        {
            return;
        }

        AddGap(SectionGap);

        var footer = new PanelContainer { ThemeTypeVariation = FooterVariation, MouseFilter = MouseFilterEnum.Ignore };
        var footerLines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };

        footerLines.AddThemeConstantOverride("separation", (int)LineGap);

        foreach (var (key, value) in rows)
        {
            footerLines.AddChild(BuildPair(key, value, null, null, KeyVariation, FooterSeparation, TextVariation));
        }

        footer.AddChild(footerLines);
        AddSection(footer);
    }

    private HBoxContainer BuildPair(string key, string value, Color? keyColor, Color? valueColor,
        string keyVariation, float separation, string valueVariation = TextVariation)
    {
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };

        row.AddThemeConstantOverride("separation", (int)separation);
        row.AddChild(BuildLabel(key, keyVariation, keyColor, isExpanding: true));
        row.AddChild(BuildLabel(value, valueVariation, valueColor));

        return row;
    }

    private static Label BuildLabel(string text, string variation, Color? color, bool isExpanding = false)
    {
        var label = new Label
        {
            Text = text,
            ThemeTypeVariation = variation,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = isExpanding ? SizeFlags.ExpandFill : SizeFlags.ShrinkEnd,
        };

        if (color != null)
        {
            label.AddThemeColorOverride("font_color", color.Value);
        }

        return label;
    }

    private Label AddLabel(string text, string variation, Color? color = null)
    {
        var label = BuildLabel(text, variation, color, isExpanding: true);

        AddSection(label);

        return label;
    }

    private void AddGap(float gap)
    {
        AddSection(new Control
        {
            CustomMinimumSize = new Vector2(0f, gap - LineGap * 2f), MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private void AddSection(Control section)
    {
        Lines.AddChild(section);
        _sections.Add(section);
    }

    private void ClearSections()
    {
        foreach (var section in _sections)
        {
            Lines.RemoveChild(section);
            section.QueueFree();
        }

        _sections.Clear();
    }

    private enum ItemRowGroup
    {
        Stats,
        Requirements,
        Bonuses,
    }
}
