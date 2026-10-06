using System.Collections.Generic;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The content families the command line converts. Conversion output is
    /// laid out under these names. Boat and tower parts convert with the
    /// characters that are built of them and land under their own name.
    /// Cursors are opt-in: their output is committed to the client theme, so a
    /// run converts them only on request.
    /// </summary>
    public static class ContentKind
    {
        public const string Character = "character";

        public const string Machine = "machine";

        public const string Item = "item";

        public const string Scene = "scene";

        public const string Table = "table";

        public const string Map = "map";

        public const string Cursor = "cursor";

        public static readonly IReadOnlyList<string> Defaults = [Character, Item, Scene, Table, Map];

        public static readonly IReadOnlyList<string> All = [..Defaults, Cursor];
    }
}
