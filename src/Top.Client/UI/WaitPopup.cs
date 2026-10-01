using Godot;

namespace Top.Client.UI;

public partial class WaitPopup : PopupBase
{
    public override void _Ready()
    {
        base._Ready();

        VisibilityChanged += UpdateCursor;
    }

    public override void _ExitTree()
    {
        if (Visible)
        {
            Input.SetDefaultCursorShape();
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("ui_accept") || inputEvent.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open()
    {
        OpenCentered();
    }

    private void UpdateCursor()
    {
        Input.SetDefaultCursorShape(Visible ? Input.CursorShape.Busy : Input.CursorShape.Arrow);
    }
}
