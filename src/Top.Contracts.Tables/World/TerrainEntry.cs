using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    public class TerrainEntry : TableEntry
    {
        [JsonProperty("texturePath")] public string TexturePath;
        [JsonProperty("type")] public int Type;
        [JsonProperty("leavesFootprints")] public bool LeavesFootprints;
    }
}
