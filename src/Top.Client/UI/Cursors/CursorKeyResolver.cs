using System.Collections.Generic;
using Godot;

namespace Top.Client.UI.Cursors;

public static class CursorKeyResolver
{
    public static readonly IReadOnlyDictionary<Input.CursorShape, CursorKey> KeysByShape =
        new Dictionary<Input.CursorShape, CursorKey>
        {
            [Input.CursorShape.Arrow] = CursorKey.Normal,
            [Input.CursorShape.Cross] = CursorKey.Normal,
            [Input.CursorShape.Help] = CursorKey.Normal,
            [Input.CursorShape.PointingHand] = CursorKey.Active,
            [Input.CursorShape.Busy] = CursorKey.Wait,
            [Input.CursorShape.Wait] = CursorKey.Wait,
            [Input.CursorShape.Drag] = CursorKey.Drag,
            [Input.CursorShape.CanDrop] = CursorKey.Drag,
            [Input.CursorShape.Move] = CursorKey.Drag,
            [Input.CursorShape.Vsize] = CursorKey.Vertical,
            [Input.CursorShape.Hsize] = CursorKey.Drag,
            [Input.CursorShape.Bdiagsize] = CursorKey.Drag,
            [Input.CursorShape.Fdiagsize] = CursorKey.Drag,
            [Input.CursorShape.Forbidden] = CursorKey.Forbid
        };

    public static CursorKey? KeyFor(Input.CursorShape shape)
    {
        return KeysByShape.GetValueOrDefault(shape);
    }

    public static CursorKey? Resolve(Input.CursorShape? controlShape, bool isWaiting, CursorKey? worldHover = null,
        CursorKey? heldAction = null)
    {
        if (heldAction != null)
        {
            return heldAction;
        }

        if (controlShape is { } shape)
        {
            return KeyFor(shape);
        }

        if (isWaiting)
        {
            return CursorKey.Wait;
        }

        return worldHover ?? CursorKey.Normal;
    }
}
