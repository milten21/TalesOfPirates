using Newtonsoft.Json;

namespace Top.Contracts.Tables.Ships
{
    public class ShipPartEntry : TableEntry
    {
        [JsonProperty("modelPath")] public string ModelPath;
        [JsonProperty("propellerIds")] public int[] PropellerIds = new int[0];
    }
}
