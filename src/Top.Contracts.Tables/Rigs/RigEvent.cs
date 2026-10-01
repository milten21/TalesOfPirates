using Newtonsoft.Json;

namespace Top.Contracts.Tables.Rigs
{
    public class RigEvent
    {
        public const string Begin = "begin";
        public const string End = "end";
        public const string Footstep = "footstep";
        public const string Hit = "hit";
        public const string Mine = "mine";
        public const string Gather = "gather";
        public const string Death = "death";
        public const string Breath = "breath";
        public const string Beat = "beat";
        public const string Mark = "mark";

        [JsonProperty("name")] public string Name;
        [JsonProperty("time")] public float Time;
    }
}
