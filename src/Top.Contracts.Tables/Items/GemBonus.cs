using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class GemBonus
    {
        [JsonProperty("text")] public string Text;
        [JsonProperty("perLevel")] public int PerLevel;
    }
}
