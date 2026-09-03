using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    public class MapEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("mapPath")] public string MapPath;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("showsAreaNames")] public bool ShowsAreaNames;
        [JsonProperty("startX")] public int StartX;
        [JsonProperty("startY")] public int StartY;
        [JsonProperty("lightDirection")] public Float3 LightDirection;
        [JsonProperty("lightColor")] public Rgb LightColor;
    }
}
