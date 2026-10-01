using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    public class AreaEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("isCity")] public bool IsCity;
    }
}
