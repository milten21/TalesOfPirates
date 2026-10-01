using Newtonsoft.Json;

namespace Top.Contracts.Tables.Characters
{
    public class CharacterEntry : TableEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("modalType")] public CharacterModalType ModalType;
        [JsonProperty("modelPath")] public string ModelPath;
        [JsonProperty("rigId")] public int? RigId;
        [JsonProperty("bodyType")] public int? BodyType;
        [JsonProperty("defaultItems")] public int[] DefaultItems = new int[0];
        [JsonProperty("partPaths")] public string[] PartPaths = new string[0];
        [JsonProperty("actionSetId")] public int? ActionSetId;
        [JsonProperty("height")] public float Height;
        [JsonProperty("radius")] public int Radius;
        [JsonProperty("territory")] public int Territory;
        [JsonProperty("seaHeight")] public int SeaHeight;
        [JsonProperty("effectAction", NullValueHandling = NullValueHandling.Ignore)] public EffectAction EffectAction;
    }
}
