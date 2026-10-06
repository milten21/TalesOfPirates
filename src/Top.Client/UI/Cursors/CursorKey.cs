using System.Collections.Generic;

namespace Top.Client.UI.Cursors;

public class CursorKey(string name, CursorKey? fallback = null)
{
    public static readonly CursorKey Normal = new("normal");

    public static readonly CursorKey Active = new("active");

    public static readonly CursorKey Click = new("click");

    public static readonly CursorKey Drag = new("drag");

    public static readonly CursorKey Vertical = new("vertical");

    public static readonly CursorKey Wait = new("wait");

    public static readonly CursorKey Forbid = new("forbid");

    public static readonly CursorKey Attack = new("attack");

    public static readonly CursorKey Skill = new("skill");

    public static readonly CursorKey Land = new("land");

    public static readonly CursorKey Board = new("board");

    public static readonly CursorKey Pick = new("pick");

    public static readonly CursorKey Camera = new("camera");

    public static readonly CursorKey Chat = new("chat");

    public static readonly CursorKey Player = new("player");

    public static readonly CursorKey Hide = new("hide");

    public static readonly CursorKey Search = new("search");

    public static readonly CursorKey Block = new("block");

    public static readonly CursorKey Repair = new("repair");

    public static readonly CursorKey Feed = new("feed");

    public static readonly IReadOnlyList<CursorKey> All =
    [
        Normal, Active, Click, Drag, Vertical, Wait, Forbid, Attack, Skill, Land,
        Board, Pick, Camera, Chat, Player, Hide, Search, Block, Repair, Feed
    ];

    public string Name { get; } = name;

    public CursorKey? Fallback { get; } = fallback;

    public override string ToString()
    {
        return Name;
    }
}
