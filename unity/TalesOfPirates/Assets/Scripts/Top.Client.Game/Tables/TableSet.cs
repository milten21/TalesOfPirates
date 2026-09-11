using Top.Contracts.Tables.World;

namespace Top.Client.Game.Tables
{
    public class TableSet
    {
        public TableSet(SceneObjectTable sceneObjectTable, TerrainTable terrainTable, MapTable mapTable)
        {
            SceneObjectTable = sceneObjectTable;
            TerrainTable = terrainTable;
            MapTable = mapTable;
        }

        public SceneObjectTable SceneObjectTable { get; }

        public TerrainTable TerrainTable { get; }

        public MapTable MapTable { get; }
    }
}
