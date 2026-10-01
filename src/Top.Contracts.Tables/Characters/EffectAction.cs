using Newtonsoft.Json;

namespace Top.Contracts.Tables.Characters
{
    public class EffectAction
    {
        [JsonProperty("keyIndex")] public int KeyIndex;
        [JsonProperty("effect")] public int Effect;
        [JsonProperty("dummy")] public int Dummy;
    }
}
