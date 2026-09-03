using Newtonsoft.Json;

namespace Top.Contracts.Tables
{
    public class TableEntry
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Include, Order = int.MinValue)]
        public int Id;
    }
}
