using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class ItemStatBonus
    {
        [JsonProperty("coefficient")] public int Coefficient;
        [JsonProperty("minimum")] public int Minimum;
        [JsonProperty("maximum")] public int Maximum;
    }
}
