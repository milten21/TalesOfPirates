using Godot;

namespace Top.Client.Features.MainMenu.Menu;

public partial class ContinuePill : Button
{
    [Export] public Control Layout { get; set; } = null!;

    [Export] public Label Detail { get; set; } = null!;

    public override void _Ready()
    {
        Layout.MinimumSizeChanged += FitLayout;

        FitLayout();
    }

    public void ShowLogin(string accountName, string serverName)
    {
        Detail.Text = $"{accountName} · {serverName}";
    }

    private void FitLayout()
    {
        CustomMinimumSize = new Vector2(Layout.GetCombinedMinimumSize().X, CustomMinimumSize.Y);
    }
}
