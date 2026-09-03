using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Top.Contracts.Tables
{
    public class TableFormat
    {
        private readonly JsonSerializer _serializer = JsonSerializer
            .Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include,
                Converters = { new StringEnumConverter() },
            });

        public List<TEntry> Read<TEntry>(Stream stream) where TEntry : TableEntry
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024, leaveOpen: true);
            using var json = new JsonTextReader(reader);

            return _serializer.Deserialize<List<TEntry>>(json) ?? new List<TEntry>();
        }

        public void Write<TEntry>(Stream stream, IEnumerable<TEntry> entries) where TEntry : TableEntry
        {
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true);
            using var json = new JsonTextWriter(writer);
            json.Formatting = Formatting.Indented;

            _serializer.Serialize(json, entries);
        }
    }
}
