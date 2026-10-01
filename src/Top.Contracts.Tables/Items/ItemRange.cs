using Newtonsoft.Json;

namespace Top.Contracts.Tables.Items
{
    public class ItemRange
    {
        [JsonProperty("minimum")] public int Minimum;
        [JsonProperty("maximum")] public int Maximum;
    }
}
