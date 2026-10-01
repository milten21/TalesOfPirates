using Godot;

namespace Top.Client.UI;

public partial class VersionLabel : Label
{
    public override void _Ready()
    {
        Text = "v" + ProjectSettings.GetSetting("application/config/version").AsString();
    }
}
