using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class ItemTypeEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
    }
}
