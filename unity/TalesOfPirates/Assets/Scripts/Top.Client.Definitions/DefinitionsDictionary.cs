using Top.Contracts.Tables.World;

namespace Top.Client.Definitions
{
    /// <summary>
    /// Typed lookups over the data tables, immutable once built.
    /// </summary>
    public class DefinitionsDictionary
    {
        public DefinitionsDictionary(SceneObjectTable sceneObjects, TerrainTable terrains, MapTable maps)
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
