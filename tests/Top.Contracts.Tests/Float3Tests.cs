using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Top.Contracts.Tables;

namespace Top.Contracts.Tests
{
    public class Float3Tests
    {
        private class VectorEntry : TableEntry
        {
            [JsonProperty("value")] public Float3 Value;
        }

        private static string Write(Float3 value)
        {
            using var stream = new MemoryStream();

            new TableFormat().Write(stream, new[] { new VectorEntry { Id = 1, Value = value } });

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static Float3 Read(string json)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return new TableFormat().Read<VectorEntry>(stream).Single().Value;
        }

        [Test]
        public void The_three_axes_write_as_lowercase_members()
        {
            var json = Write(new Float3(1f, 2f, -3f));

            Assert.That(json, Does.Contain("\"x\": 1.0"));
            Assert.That(json, Does.Contain("\"y\": 2.0"));
            Assert.That(json, Does.Contain("\"z\": -3.0"));
        }

        [Test]
        public void A_written_vector_reads_back_the_same()
        {
            Assert.That(Read(Write(new Float3(1f, 2f, -3f))), Is.EqualTo(new Float3(1f, 2f, -3f)));
        }

        [Test]
        public void An_absent_vector_reads_as_the_origin()
        {
            Assert.That(Read("""[ { "id": 1 } ]"""), Is.EqualTo(new Float3(0f, 0f, 0f)));
        }
    }
}
