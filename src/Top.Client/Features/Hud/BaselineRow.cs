using System.Collections.Generic;
using Godot;

namespace Top.Client.Features.Hud;

public partial class BaselineRow : Container
{
    private static readonly StringName SeparationConstant = "separation";

    private static readonly StringName FontName = "font";

    private static readonly StringName FontSizeName = "font_size";

    private static readonly StringName NormalStyle = "normal";

    public float Baseline => MeasureLine().Ascent;

    public override Vector2 _GetMinimumSize()
    {
        var line = MeasureLine();

        return new Vector2(line.Width, line.Ascent + line.Descent);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            Arrange();
        }
    }

    private static float MeasureAscent(Control child)
    {
        return child switch
        {
            Label label => label.GetThemeStylebox(NormalStyle).GetMargin(Side.Top)
                           + label.GetThemeFont(FontName).GetAscent(label.GetThemeFontSize(FontSizeName)),
            BaselineRow row => row.Baseline,
            _ => child.GetCombinedMinimumSize().Y
        };
    }

    private static bool IsExpanding(Control child)
    {
        return (child.SizeFlagsHorizontal & SizeFlags.Expand) != 0;
    }

    private List<Control> CollectPlacedChildren()
    {
        var children = new List<Control>();

        foreach (var node in GetChildren())
        {
            if (node is Control { Visible: true, TopLevel: false } child)
            {
                children.Add(child);
            }
        }

        return children;
    }

    private Line MeasureLine()
    {
        var children = CollectPlacedChildren();
        var width = (float)GetThemeConstant(SeparationConstant) * Mathf.Max(children.Count - 1, 0);
        var ascent = 0f;
        var descent = 0f;

        foreach (var child in children)
        {
            var minimumSize = child.GetCombinedMinimumSize();
            var childAscent = MeasureAscent(child);

            width += minimumSize.X;
            ascent = Mathf.Max(ascent, childAscent);
            descent = Mathf.Max(descent, minimumSize.Y - childAscent);
        }

        return new Line(width, ascent, descent);
    }

    private void Arrange()
    {
        var children = CollectPlacedChildren();
        var line = MeasureLine();
        var expandingCount = children.FindAll(IsExpanding).Count;
        var extraWidth = expandingCount > 0 ? Mathf.Max(Size.X - line.Width, 0f) / expandingCount : 0f;
        var separation = GetThemeConstant(SeparationConstant);
        var left = 0f;

        foreach (var child in children)
        {
            var minimumSize = child.GetCombinedMinimumSize();
            var width = minimumSize.X + (IsExpanding(child) ? extraWidth : 0f);

            FitChildInRect(child, new Rect2(left, line.Ascent - MeasureAscent(child), width, minimumSize.Y));

            left += width + separation;
        }
    }

    private record Line(float Width, float Ascent, float Descent);
}
