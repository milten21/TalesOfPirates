using Newtonsoft.Json;

namespace Top.Contracts.Tables.Characters
{
    public class PortraitCameraEntry : TableEntry
    {
        [JsonProperty("angle")] public int Angle;
        [JsonProperty("distance")] public float Distance;
        [JsonProperty("height")] public float Height;
    }
}
