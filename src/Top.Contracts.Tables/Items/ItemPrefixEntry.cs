using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class ItemPrefixEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
    }
}
