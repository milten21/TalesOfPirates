using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    /// <summary>
    /// Represents a map entry in the map info table.
    /// </summary>
    public class MapEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("mapPath")] public string MapPath;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("showsAreaNames")] public bool ShowsAreaNames;
        [JsonProperty("startX")] public int StartX;
        [JsonProperty("startY")] public int StartY;
        [JsonProperty("lightDirection")] public float[] LightDirection;
        [JsonProperty("lightColor")] public float[] LightColor;
    }
}
