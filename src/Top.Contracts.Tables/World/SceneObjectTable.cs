using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class SceneObjectTable : Table<SceneObjectEntry>
    {
        public const string Path = "tables/sceneobjects.json";

        public SceneObjectTable(IEnumerable<SceneObjectEntry> entries) : base(entries)
        {
        }
    }
}
