using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    /// <summary>
    /// One terrain a map can paint with: the id its tiles carry and the texture
    /// that draws it.
    /// </summary>
    public class TerrainEntry : TableEntry
    {
        [JsonProperty("texturePath")] public string TexturePath;
        [JsonProperty("type")] public int Type;
        [JsonProperty("leavesFootprints")] public bool LeavesFootprints;
    }
}
