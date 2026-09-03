using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Contracts.Tables.World
{
    public class SceneObjectEntryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SceneObjectEntry);
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            var entry = Blank(json, serializer);

            serializer.Populate(json.CreateReader(), entry);

            return entry;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        private static SceneObjectEntry Blank(JObject json, JsonSerializer serializer)
        {
            return json["kind"]?.ToObject<SceneObjectKind>(serializer) switch
            {
                SceneObjectKind.PointLight => new PointLightEntry(),
                SceneObjectKind.AmbientLight => new AmbientLightEntry(),
                SceneObjectKind.Fog => new FogEntry(),
                SceneObjectKind.Sound => new SoundEntry(),
                _ => json["sequence"] != null ? new FadeEntry() : new SceneObjectEntry(),
            };
        }
    }
}
