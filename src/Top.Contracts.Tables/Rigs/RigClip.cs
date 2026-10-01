using Newtonsoft.Json;

namespace Top.Contracts.Tables.Rigs
{
    public class RigClip
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("file")] public string File;
        [JsonProperty("animation")] public string Animation;
    }
}
