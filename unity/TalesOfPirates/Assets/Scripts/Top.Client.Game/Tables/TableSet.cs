using Top.Contracts.Tables.World;

namespace Top.Client.Game.Tables
{
    public class TableSet
    {
        public TableSet(SceneObjectTable sceneObjects, TerrainTable terrains, MapTable maps)
        {
            SceneObjects = sceneObjects;
            Terrains = terrains;
            Maps = maps;
        }

        public SceneObjectTable SceneObjects { get; }

        public TerrainTable Terrains { get; }

        public MapTable Maps { get; }
    }
}
