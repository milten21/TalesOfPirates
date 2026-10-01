using Godot;

namespace Top.Client.UI;

public partial class IconAction : CaptionButton
{
    [Export] public Control Layout { get; set; } = null!;

    [Export] public Label IconLabel { get; set; } = null!;

    public override void _Ready()
    {
        Layout.MinimumSizeChanged += FitLayout;

        FitLayout();
    }

    protected override void ApplyFontColor(Color color)
    {
        base.ApplyFontColor(color);

        IconLabel.AddThemeColorOverride("font_color", color);
    }

    private void FitLayout()
    {
        CustomMinimumSize = Layout.GetCombinedMinimumSize();
    }
}
