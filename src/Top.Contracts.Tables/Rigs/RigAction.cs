using Newtonsoft.Json;

namespace Top.Contracts.Tables.Rigs
{
    public class RigAction
    {
        [JsonProperty("id")] public int Id;
        [JsonProperty("stance", NullValueHandling = NullValueHandling.Ignore)] public Stance? Stance;
        [JsonProperty("clip")] public string Clip;
        [JsonProperty("events", NullValueHandling = NullValueHandling.Ignore)] public RigEvent[] Events;
    }
}
