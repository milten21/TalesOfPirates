using Newtonsoft.Json;

namespace Top.Contracts
{
    public struct Rgb
    {
        [JsonProperty("r")] public byte R;
        [JsonProperty("g")] public byte G;
        [JsonProperty("b")] public byte B;

        public Rgb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}
