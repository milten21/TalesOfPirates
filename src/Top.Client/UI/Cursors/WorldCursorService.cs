using System;
using Godot;

namespace Top.Client.UI.Cursors;

public class WorldCursorService(CursorSkin skin)
{
    private WorldCursor? _shown;

    private WorldCursor? _held;

    private bool _isStarted;

    public bool IsHolding => _held != null;

    public void Start(SceneTree tree)
    {
        if (_isStarted)
        {
            return;
        }

        _isStarted = true;

        tree.NodeAdded += PointAtButton;
        tree.ProcessFrame += () => Update(tree);
    }

    public void Show(WorldCursor cursor)
    {
        _shown = cursor;
    }

    public void Hold(WorldCursor cursor)
    {
        _held = cursor;
    }

    public void Release()
    {
        _held = null;
    }

    public CursorKey? Resolve(Input.CursorShape? controlShape, bool isWaiting)
    {
        return CursorKeyResolver.Resolve(controlShape, isWaiting, KeyFor(_shown), KeyFor(_held));
    }

    public void EndFrame()
    {
        _shown = null;
    }

    private static void PointAtButton(Node node)
    {
        if (node is BaseButton { MouseDefaultCursorShape: Control.CursorShape.Arrow } button)
        {
            button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        }
    }

    private static CursorKey? KeyFor(WorldCursor? cursor)
    {
        return cursor switch
        {
            null => null,
            WorldCursor.Attack => CursorKey.Attack,
            WorldCursor.Skill => CursorKey.Skill,
            WorldCursor.Land => CursorKey.Land,
            WorldCursor.Board => CursorKey.Board,
            WorldCursor.Pick => CursorKey.Pick,
            WorldCursor.Camera => CursorKey.Camera,
            WorldCursor.Chat => CursorKey.Chat,
            WorldCursor.Player => CursorKey.Player,
            WorldCursor.Hide => CursorKey.Hide,
            WorldCursor.Search => CursorKey.Search,
            WorldCursor.Block => CursorKey.Block,
            WorldCursor.Repair => CursorKey.Repair,
            WorldCursor.Feed => CursorKey.Feed,
            WorldCursor.Forbid => CursorKey.Forbid,
            WorldCursor.Active => CursorKey.Active,
            _ => throw new ArgumentOutOfRangeException(nameof(cursor), cursor, null)
        };
    }

    private void Update(SceneTree tree)
    {
        var control = tree.Root.GuiGetHoveredControl();

        Input.CursorShape? controlShape = control == null
            ? null
            : (Input.CursorShape)control.GetCursorShape(control.GetLocalMousePosition());

        var isWaiting = control == null && Input.GetCurrentCursorShape() is Input.CursorShape.Busy
            or Input.CursorShape.Wait;

        var key = IsHolding || control == null ? Resolve(controlShape, isWaiting) : null;

        var isDisabledButton = !IsHolding && control is BaseButton { Disabled: true };

        if (isDisabledButton)
        {
            key = CursorKey.Forbid;
        }

        skin.Replace(key, IsHolding || isDisabledButton);

        EndFrame();
    }
}
