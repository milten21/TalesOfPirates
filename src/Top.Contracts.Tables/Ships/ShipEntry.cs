using Newtonsoft.Json;

namespace Top.Contracts.Tables.Ships
{
    public class ShipEntry : TableEntry
    {
        [JsonProperty("hullId")] public int HullId;
    }
}
