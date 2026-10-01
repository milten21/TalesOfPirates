using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class GemEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("itemId")] public int ItemId;
        [JsonProperty("color")] public int Color;
        [JsonProperty("bonus")] public GemBonus Bonus;
        [JsonProperty("fits")] public int[] Fits = new int[0];
        [JsonProperty("maxLevel")] public int MaxLevel;
    }
}
