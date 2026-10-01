using Newtonsoft.Json;

namespace Top.Contracts.Tables.Characters
{
    public class JobEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
    }
}
