using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    [JsonConverter(typeof(SceneObjectEntryConverter))]
    public class SceneObjectEntry : TableEntry
    {
        [JsonProperty("modelPath")] public string ModelPath;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("kind")] public SceneObjectKind Kind;
        [JsonProperty("attachEffectId")] public int AttachEffectId;
        [JsonProperty("enableEnvLight")] public bool EnableEnvLight;
        [JsonProperty("enablePointLight")] public bool EnablePointLight;
        [JsonProperty("style")] public int Style;
        [JsonProperty("flag")] public int Flag;
        [JsonProperty("sizeFlag")] public int SizeFlag;
        [JsonProperty("shadeFlag")] public bool ShadeFlag;
        [JsonProperty("isReallyBig")] public bool IsReallyBig;
    }


    public class PointLightEntry : SceneObjectEntry
    {
        [JsonProperty("color")] public Rgb Color;
        [JsonProperty("range")] public int Range;
        [JsonProperty("attenuation")] public float Attenuation;
        [JsonProperty("animationId")] public int AnimationId;
    }

    public class AmbientLightEntry : SceneObjectEntry
    {
        [JsonProperty("color")] public Rgb Color;
    }

    public class FogEntry : SceneObjectEntry
    {
        [JsonProperty("color")] public Rgb Color;
    }

    public class SoundEntry : SceneObjectEntry
    {
        [JsonProperty("sound")] public string Sound;
        [JsonProperty("distance")] public int Distance;
    }

    public class FadeEntry : SceneObjectEntry
    {
        [JsonProperty("sequence")] public int[] Sequence;
        [JsonProperty("coefficient")] public float Coefficient;
    }
}
