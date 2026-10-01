using Godot;

namespace Top.Client.UI;

public static class NavigationInput
{
    public static bool IsNavigation(InputEvent inputEvent)
    {
        return inputEvent.IsActionPressed("ui_left") || inputEvent.IsActionPressed("ui_right")
            || inputEvent.IsActionPressed("ui_up") || inputEvent.IsActionPressed("ui_down")
            || inputEvent.IsActionPressed("ui_focus_next") || inputEvent.IsActionPressed("ui_focus_prev");
    }
}
