using Godot;

namespace Top.Client.UI;

public partial class ScrollList : Control
{
    private static readonly StringName ScrollBarVariation = "ListScrollBar";

    [Export] public TextureRect Mask { get; set; } = null!;

    [Export] public ScrollContainer Scroll { get; set; } = null!;

    [Export] public Container Items { get; set; } = null!;

    [Export] public float MaximumHeight { get; set; } = 388f;

    [Export] public float TopFadeHeight { get; set; } = 28f;

    [Export] public float BottomFadeHeight { get; set; } = 36f;

    private VScrollBar ScrollBar => Scroll.GetVScrollBar();

    public override void _Ready()
    {
        ScrollBar.ThemeTypeVariation = ScrollBarVariation;

        Items.MinimumSizeChanged += FitHeight;
        Mask.Resized += ShowFades;
        ScrollBar.Changed += ShowFades;
        ScrollBar.ValueChanged += _ => ShowFades();

        FitHeight();
    }

    public override void _EnterTree()
    {
        GetViewport().GuiFocusChanged += FollowFocus;
    }

    public override void _ExitTree()
    {
        GetViewport().GuiFocusChanged -= FollowFocus;
    }

    public void ScrollToTop()
    {
        Scroll.ScrollVertical = 0;
    }

    private void FitHeight()
    {
        float height = Mathf.Min(Items.GetCombinedMinimumSize().Y, MaximumHeight);

        CustomMinimumSize = new Vector2(CustomMinimumSize.X, height);
    }

    private void ShowFades()
    {
        float height = Mask.Size.Y;

        if (height <= 0f || Mask.Texture is not GradientTexture2D texture)
        {
            return;
        }

        bool isScrolled = ScrollBar.Value > 0.0;
        bool hasMoreBelow = ScrollBar.Value + ScrollBar.Page < ScrollBar.MaxValue;

        texture.Height = Mathf.CeilToInt(height);

        texture.Gradient.Offsets = [0f, TopFadeHeight / height, 1f - BottomFadeHeight / height, 1f];
        texture.Gradient.Colors =
        [
            isScrolled ? Colors.Transparent : Colors.White,
            Colors.White,
            Colors.White,
            hasMoreBelow ? Colors.Transparent : Colors.White
        ];
    }

    private void FollowFocus(Control focused)
    {
        if (Items.IsAncestorOf(focused))
        {
            Callable.From(() => ScrollTo(focused)).CallDeferred();
        }
    }

    private void ScrollTo(Control focused)
    {
        if (!IsInstanceValid(focused) || FindItem(focused) is not { } item)
        {
            return;
        }

        float top = item.Position.Y - TopFadeHeight;
        float bottom = item.Position.Y + item.Size.Y + BottomFadeHeight;

        if (top < Scroll.ScrollVertical)
        {
            Scroll.ScrollVertical = Mathf.FloorToInt(Mathf.Max(top, 0f));
        }
        else if (bottom > Scroll.ScrollVertical + Scroll.Size.Y)
        {
            Scroll.ScrollVertical = Mathf.CeilToInt(bottom - Scroll.Size.Y);
        }
    }

    private Control? FindItem(Node node)
    {
        while (node.GetParent() is { } parent && parent != Items)
        {
            node = parent;
        }

        return node.GetParent() == Items ? node as Control : null;
    }
}
