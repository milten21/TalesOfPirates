using Newtonsoft.Json;

namespace Top.Contracts.Tables.World
{
    public class TipEntry : TableEntry
    {
        [JsonProperty("text")] public string Text;
    }
}
